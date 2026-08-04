/* admin-users.js
   All client-side behaviour for the Manage Users screen.
   Nothing here is inline in the .cshtml — this file is referenced via
   <script src="~/js/admin-users.js"></script> from Users.cshtml.
   Requires jQuery + Bootstrap 5 (for the modal) already loaded by the layout.
*/
(function () {
    "use strict";

    var $form = $("#usersFilterForm");
    var $container = $("#usersGridContainer");
    var $summary = $("#usersSummary");
    var gridUrl = $form.data("grid-url");

    // ------------------------------------------------------------------
    // Core: fetch the grid partial and swap it in. Everything else in this
    // file just decides WHAT query string to send, and calls this.
    // ------------------------------------------------------------------
    function loadGrid(params) {
        $.ajax({
            url: gridUrl,
            method: "GET",
            data: params,
            success: function (html) {
                $container.html(html);
                updateSummary();
            },
            error: function () {
                $container.html('<div class="alert alert-danger">Failed to load users. Please try again.</div>');
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

    // ------------------------------------------------------------------
    // Search form submit (button click or Enter key) — AJAX, no reload.
    // ------------------------------------------------------------------
    $form.on("submit", function (e) {
        e.preventDefault();
        $form.find('input[name="Filter.PageNumber"]').val(1); // new search always starts at page 1
        loadGrid($form.serialize());
    });

    $("#usersClearFilters").on("click", function () {
        $form.find('input[type="text"], input[type="number"]').val("");
        $form.find('select[name="Filter.RoleId"]').val("");
        $form.find('input[name="Filter.SortBy"]').val("ConsumerId");
        $form.find('input[name="Filter.SortDir"]').val("DESC");
        $form.find('input[name="Filter.PageNumber"]').val(1);
        loadGrid($form.serialize());
    });

    // Page-size change re-searches immediately.
    $form.on("change", 'select[name="Filter.PageSize"]', function () {
        $form.find('input[name="Filter.PageNumber"]').val(1);
        loadGrid($form.serialize());
    });

    // ------------------------------------------------------------------
    // Sortable column headers (delegated — headers are re-rendered on
    // every grid refresh, so binding must survive that).
    // ------------------------------------------------------------------
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

    // ------------------------------------------------------------------
    // Pagination links (delegated).
    // ------------------------------------------------------------------
    $container.on("click", ".js-page", function () {
        if ($(this).closest("li").hasClass("disabled")) return;
        var page = $(this).data("page");
        $form.find('input[name="Filter.PageNumber"]').val(page);
        loadGrid($form.serialize());
    });

    // ------------------------------------------------------------------
    // Delete — confirm, AJAX POST, then refresh the grid in place.
    // ------------------------------------------------------------------
    $container.on("click", ".js-delete-user", function () {
        var id = $(this).data("id");
        var name = $(this).data("name") || "this user";

        confirmDialog('Are you sure you want to delete user ' + name + '?', function () {
            $.ajax({
                url: $container.data('delete-url') || window.adminUsersUrls.deleteUser,
                method: "POST",
                data: { id: id },
                headers: getAntiForgeryHeader(),
                success: function (res) {
                    if (res && res.success) {
                        loadGrid($form.serialize());
                    } else {
                        alert((res && res.message) || "Failed to delete user.");
                    }
                },
                error: function (xhr) {
                    var msg = (xhr.responseJSON && xhr.responseJSON.message) || "Failed to delete user.";
                    alert(msg);
                }
            });
        });
    });

    // ------------------------------------------------------------------
    // Edit modal: load form via AJAX, save via AJAX, refresh grid on success.
    // ------------------------------------------------------------------
    $container.on("click", ".js-edit-user", function () {
        var id = $(this).data("id");
        $.ajax({
            url: window.adminUsersUrls.editUser,
            method: "GET",
            data: { id: id },
            success: function (html) {
                $("#editUserModalContent").html(html);
                var modal = new bootstrap.Modal(document.getElementById("editUserModal"));
                modal.show();
            },
            error: function () {
                alert("Failed to load user details.");
            }
        });
    });

    // Delegated on document because the modal content (and its Save button)
    // is injected after page load.
    $(document).on("click", "#saveUserBtn", function () {
        var $btn = $(this);
        var url = $btn.data("url");
        var $errBox = $("#editUserFormError");
        $errBox.text("");

        var payload = {
            consumerId: $("#editUserForm [name=consumerId]").val(),
            roleId: $("#editUserForm [name=roleId]").val(),
            deptId: $("#editUserForm [name=deptId]").val() || null,
            wardId: $("#editUserForm [name=wardId]").val() || null,
            designation: $("#editUserForm [name=designation]").val()
        };

        $btn.prop("disabled", true);

        $.ajax({
            url: url,
            method: "POST",
            data: payload,
            headers: getAntiForgeryHeader(),
            success: function (res) {
                if (res && res.success) {
                    bootstrap.Modal.getInstance(document.getElementById("editUserModal")).hide();
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

    // If your app uses ASP.NET MVC's AntiForgeryToken, wire it here so every
    // POST above carries it. No-op if the hidden token field isn't present.
    function getAntiForgeryHeader() {
        var token = $('input[name="__RequestVerificationToken"]').val();
        return token ? { "RequestVerificationToken": token } : {};
    }

    updateSummary();
})();
