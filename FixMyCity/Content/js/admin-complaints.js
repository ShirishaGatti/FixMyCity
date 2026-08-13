/* admin-complaints.js
   Same AJAX search/sort/page/assign/delete pattern as admin-users.js, applied
   to the Manage Complaints screen. Kept as a separate file (rather than one
   shared generic script) so each page's URLs/selectors stay simple and
   explicit  worth merging later only if a third grid shows up.
*/
(function () {
    "use strict";

    var $form = $("#complaintsFilterForm");
    var $container = $("#complaintsGridContainer");
    var $summary = $("#complaintsSummary");
    var gridUrl = $form.data("grid-url");

    function loadGrid(params) {
        $.ajax({
            url: gridUrl,
            method: "GET",
            data: params,
            cache: false,
            success: function (html) {
                $container.html(html);
                updateSummary();
            },
            error: function () {
                $container.html('<div class="alert alert-danger">Failed to load complaints. Please try again.</div>');
            }
        });
    }

    function updateSummary() {
        var $tbl = $container.find(".table-responsive");
        var total = parseInt($tbl.data("total-count"), 10) || 0;
        var page = parseInt($tbl.data("page"), 10) || 1;
        var pageSize = parseInt($tbl.data("page-size"), 10) || 10;
        if (total === 0) { $summary.text(""); return; }
        var start = (page - 1) * pageSize + 1;
        var end = Math.min(page * pageSize, total);
        $summary.text("Showing " + start + "-" + end + " of " + total);
    }

    $form.on("submit", function (e) {
        e.preventDefault();
        $form.find('input[name="Filter.PageNumber"]').val(1);
        loadGrid($form.serialize());
    });

    $("#complaintsClearFilters").on("click", function () {
        $form.find('input[type="text"], input[type="number"]').val("");
        $form.find('select').val("").prop('selectedIndex', 0);
        $form.find('input[name="Filter.SortBy"]').val("ComplaintId");
        $form.find('input[name="Filter.SortDir"]').val("DESC");
        $form.find('input[name="Filter.PageNumber"]').val(1);
        loadGrid($form.serialize());
    });

    $form.on("change", 'select[name="Filter.PageSize"]', function () {
        $form.find('input[name="Filter.PageNumber"]').val(1);
        loadGrid($form.serialize());
    });

    $container.on("click", ".sortable-th", function () {
        var col = $(this).data("sort-col");
        var $tbl = $(this).closest(".table-responsive");
        var currentSort = $tbl.data("sort-by");
        var currentDir = $tbl.data("sort-dir");
        var newDir = (currentSort === col && currentDir === "ASC") ? "DESC" : "ASC";

        $form.find('input[name="Filter.SortBy"]').val(col);
        $form.find('input[name="Filter.SortDir"]').val(newDir);
        $form.find('input[name="Filter.PageNumber"]').val(1);

        loadGrid($form.serialize());
    });

    $container.on("click", ".js-page", function () {
        if ($(this).closest("li").hasClass("disabled")) return;
        var page = $(this).data("page");
        $form.find('input[name="Filter.PageNumber"]').val(page);
        loadGrid($form.serialize());
    });

    $container.on("click", ".js-delete-complaint", function () {
        var id = $(this).data("id");
        var title = $(this).data("title") || "this complaint";

        confirmDialog('Are you sure you want to delete complaint "' + title + '"?', function () {
           /* confirmDialog('Are you sure you want to delete this complaint?', function () {
                $.ajax({
                    url: '/Complaint/DeleteComplaint/' + id, type: 'POST',
                    data: {
                        __RequestVerificationToken: antiForgeryToken
                    },
                    success: function (res) {
                        if (res.success) window.location.reload();
                        else alert(res.message);
                    },
                    error: function () { alert('Failed to delete complaint.'); }
                });
            });*/
            $.ajax({
                url: window.adminComplaintsUrls.deleteComplaint,
                method: "POST",
                data: {
                    id: id,
                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (res) {
                    if (res && res.success) {
                        showToast('Complaint deleted successfully.', 'error', 'Deleted');
                        loadGrid($form.serialize());
                    } else {
                        showToast((res && res.message) || "Failed to delete complaint.", 'error', 'Error');
                    }
                },
                error: function (xhr) {
                    var msg = (xhr.responseJSON && xhr.responseJSON.message) || "Failed to delete complaint. Status: " + xhr.status;
                    showToast(msg, 'error', 'Error');
                }
            });
        });
    });

    window.closeAssignComplaintModal = function () {
        var m = document.getElementById("assignComplaintModal");
        if (m) m.style.display = "none";
    };

    function openAssignModal(id) {
        if (!id) {
            showToast("Missing complaint id.", 'error', 'Error');
            return;
        }

        $.ajax({
            url: window.adminComplaintsUrls.assignComplaint,
            method: "GET",
            data: { id: id },
            success: function (html) {
                var $modalContent = $("#assignComplaintModalContent");
                var modalEl = document.getElementById("assignComplaintModal");
                if (!$modalContent.length || !modalEl) {
                    showToast("Assign modal markup is missing from the page.", 'error', 'Error');
                    return;
                }
                $modalContent.html(html);
                modalEl.style.display = "flex";

                // Attach handlers DIRECTLY to the injected elements.
                // We cannot use $(document).on() delegation here because the modal
                // content div has onclick="event.stopPropagation()" which kills
                // all bubbling before it reaches document.

                $modalContent.find("#assignOfficerSelect").on("change", function () {
                    var $chip = $("#assignOfficerLive");
                    var $name = $("#assignOfficerLiveName");
                    if (!$chip.length || !$name.length) return;
                    var $opt = $(this).find("option:selected");
                    var officerName = $opt.val() ? $opt.text().split(" - ")[0] : "";
                    $chip.toggleClass("unassigned", !officerName)
                         .find("i").attr("class", "bi " + (officerName ? "bi-person-check-fill" : "bi-person-dash"));
                    $name.text(officerName ? officerName : "Unassigned");
                });

                $modalContent.find("#assignComplaintBtn").on("click", function () {
                    var $btn = $(this);
                    var $errBox = $("#assignComplaintFormError");
                    $errBox.text("");

                    try {
                        var token = $('input[name="__RequestVerificationToken"]').first().val();

                        var payload = {
                            complaintId: $("#assignComplaintForm [name=complaintId]").val(),
                            assignedTo: $("#assignComplaintForm [name=assignedTo]").val() || null,
                            __RequestVerificationToken: token
                        };

                        $btn.prop("disabled", true);

                        $.ajax({
                            url: $btn.data("url"),
                            method: "POST",
                            data: payload,
                            success: function (res) {
                                console.log("[assign] success", res);
                                if (res && res.success) {
                                    window.closeAssignComplaintModal();
                                    showToast('Complaint assigned successfully!', 'success', 'Saved');
                                    loadGrid($form.serialize());
                                } else {
                                    var msg = (res && res.message) || "Failed to assign complaint.";
                                    $errBox.text(msg);
                                    showToast(msg, 'error', 'Error');
                                }
                            },
                            error: function (xhr) {
                                console.log("[assign] error", xhr);
                                var msg = (xhr.responseJSON && xhr.responseJSON.message) || "Failed to assign complaint. Status: " + xhr.status;
                                $errBox.text(msg);
                                showToast(msg, 'error', 'Error');
                            },
                            complete: function () {
                                $btn.prop("disabled", false);
                            }
                        });
                    } catch (ex) {
                        $btn.prop("disabled", false);
                        console.log("[assign] exception", ex);
                        showToast("Assign failed: " + ex.message, 'error', 'Error');
                    }
                });
            },
            error: function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.message) || "Failed to load complaint details.";
                showToast(msg, 'error', 'Error');
            }
        });
    }

    // Bind on document (not just the grid container) so the handler survives any
    // container re-render, and use a namespace so re-running the script never
    // double-binds. try/catch makes any failure visible instead of silent.
    $(document).off("click.assignComplaint").on("click.assignComplaint", ".js-assign-complaint", function () {
        try {
            openAssignModal($(this).data("id"));
        } catch (ex) {
            showToast("Assign failed: " + ex.message, 'error', 'Error');
        }
    });

    function getAntiForgeryHeader() {
        var token = $('input[name="__RequestVerificationToken"]').first().val();
        return token ? { "__RequestVerificationToken": token } : {};
    }

    updateSummary();
})();

