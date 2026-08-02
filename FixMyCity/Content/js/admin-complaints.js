/* admin-complaints.js
   Same AJAX search/sort/page/edit/delete pattern as admin-users.js, applied
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
        $container.css("opacity", 0.5);
        $.ajax({
            url: gridUrl,
            method: "GET",
            data: params,
            success: function (html) {
                $container.html(html);
                updateSummary();
            },
            error: function () {
                $container.html('<div class="alert alert-danger">Failed to load complaints. Please try again.</div>');
            },
            complete: function () {
                $container.css("opacity", 1);
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
        $form.find('select[name="Filter.CategoryId"]').val("");
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

        if (!confirm("Delete complaint \"" + title + "\"? This cannot be undone.")) return;

        $.ajax({
            url: window.adminComplaintsUrls.deleteComplaint,
            method: "POST",
            data: { id: id },
            headers: getAntiForgeryHeader(),
            success: function (res) {
                if (res && res.success) {
                    loadGrid($form.serialize());
                } else {
                    alert((res && res.message) || "Failed to delete complaint.");
                }
            },
            error: function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.message) || "Failed to delete complaint.";
                alert(msg);
            }
        });
    });

    $container.on("click", ".js-edit-complaint", function () {
        var id = $(this).data("id");
        $.ajax({
            url: window.adminComplaintsUrls.editComplaint,
            method: "GET",
            data: { id: id },
            success: function (html) {
                $("#editComplaintModalContent").html(html);
                var modal = new bootstrap.Modal(document.getElementById("editComplaintModal"));
                modal.show();
            },
            error: function () {
                alert("Failed to load complaint details.");
            }
        });
    });

    $(document).on("click", "#saveComplaintBtn", function () {
        var $btn = $(this);
        var url = $btn.data("url");
        var $errBox = $("#editComplaintFormError");
        $errBox.text("");

        var payload = {
            complaintId: $("#editComplaintForm [name=complaintId]").val(),
            categoryId: $("#editComplaintForm [name=categoryId]").val(),
            priorityId: $("#editComplaintForm [name=priorityId]").val(),
            statusId: $("#editComplaintForm [name=statusId]").val(),
            assignedTo: $("#editComplaintForm [name=assignedTo]").val() || null
        };

        $btn.prop("disabled", true);

        $.ajax({
            url: url,
            method: "POST",
            data: payload,
            headers: getAntiForgeryHeader(),
            success: function (res) {
                if (res && res.success) {
                    bootstrap.Modal.getInstance(document.getElementById("editComplaintModal")).hide();
                    loadGrid($form.serialize());
                } else {
                    $errBox.text((res && res.message) || "Failed to save changes.");
                }
            },
            error: function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.message) || "Failed to save changes.";
                $errBox.text(msg);
            },
            complete: function () {
                $btn.prop("disabled", false);
            }
        });
    });

    function getAntiForgeryHeader() {
        var token = $('input[name="__RequestVerificationToken"]').val();
        return token ? { "RequestVerificationToken": token } : {};
    }

    updateSummary();
})();
