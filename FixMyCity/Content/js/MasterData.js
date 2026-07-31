/* =============================================================
   MasterData.js  –  Card-based master data page driver
   7 entity cards → click → slide panel with live data table + modal save
   Bootstrap 5 + jQuery.
   ============================================================= */

var MasterData = (function ($) {
    'use strict';

    /* ── Config: one entry per entity ─────────────────────────── */
    var cfg = {
        state: { label: 'State', icon: 'bi-geo-fill', gradient: 'linear-gradient(135deg,#1574e0,#38bdf8)', hasParent: false, parentEntity: null, parentLabel: null },
        district: { label: 'District', icon: 'bi-map-fill', gradient: 'linear-gradient(135deg,#7c3aed,#a78bfa)', hasParent: true, parentEntity: 'state', parentLabel: 'State' },
        city: { label: 'City', icon: 'bi-building-fill', gradient: 'linear-gradient(135deg,#0891b2,#22d3ee)', hasParent: true, parentEntity: 'district', parentLabel: 'District' },
        ward: { label: 'Ward', icon: 'bi-signpost-2-fill', gradient: 'linear-gradient(135deg,#d97706,#fbbf24)', hasParent: true, parentEntity: 'city', parentLabel: 'City' },
        category: { label: 'Category', icon: 'bi-tags-fill', gradient: 'linear-gradient(135deg,#059669,#34d399)', hasParent: false, parentEntity: null, parentLabel: null },
        department: { label: 'Department', icon: 'bi-building-gear', gradient: 'linear-gradient(135deg,#be185d,#f472b6)', hasParent: false, parentEntity: null, parentLabel: null },
        role: { label: 'Role', icon: 'bi-shield-lock-fill', gradient: 'linear-gradient(135deg,#dc2626,#f87171)', hasParent: false, parentEntity: null, parentLabel: null }
    };

    var urls = { list: '', save: '' };
    var activeEntity = null;
    var modalInstance = null;

    /* ── Init ─────────────────────────────────────────────────── */
    function init(options) {
        urls = options.urls;

        // Bootstrap modal instance (BS5 native API)
        var modalEl = document.getElementById('masterModal');
        if (modalEl && window.bootstrap) {
            modalInstance = new bootstrap.Modal(modalEl);
        }

        // Card clicks
        $('#mdCards .md-card').on('click keydown', function (e) {
            if (e.type === 'keydown' && e.which !== 13 && e.which !== 32) return;
            e.preventDefault();
            var entity = $(this).data('entity');
            if (activeEntity === entity) {
                closePanel();          // toggle: second click closes
            } else {
                openPanel(entity);
            }
        });

        // Panel "Add New" button
        $('#btnAddRecord').on('click', function () {
            if (activeEntity) openModal(activeEntity, null);
        });

        // Panel close button
        $('#btnClosePanel').on('click', closePanel);

        // Form submit
        $('#masterForm').on('submit', function (e) {
            e.preventDefault();
            submitForm();
        });

        // Clear error on modal hide
        $('#masterModal').on('hidden.bs.modal', function () {
            clearFormError();
        });
    }

    /* ── Panel open / close ───────────────────────────────────── */
    function openPanel(entity) {
        var c = cfg[entity];

        // Highlight active card
        $('#mdCards .md-card').removeClass('active').attr('aria-expanded', 'false');
        $('#card-' + entity).addClass('active').attr('aria-expanded', 'true');

        // Set panel icon + title
        $('#mdPanelIcon').css('background', c.gradient).html('<i class="bi ' + c.icon + '"></i>');
        $('#mdPanelTitle').text(c.label + 's');
        $('#mdPanelSubtitle').text('Manage ' + c.label.toLowerCase() + ' records');

        activeEntity = entity;

        // Show panel, load data
        var $panel = $('#mdPanel');
        $panel.show();

        // Smooth scroll to panel on small screens
        $('html, body').animate({ scrollTop: $panel.offset().top - 20 }, 300);

        loadGrid(entity);
    }

    function closePanel() {
        $('#mdCards .md-card').removeClass('active').attr('aria-expanded', 'false');
        $('#mdPanel').hide();
        activeEntity = null;
    }

    /* ── Grid load / render ───────────────────────────────────── */
    function loadGrid(entity) {
        var $body = $('#mdPanelBody');
        $body.html(loadingHtml());

        $.ajax({
            url: urls.list,
            type: 'GET',
            data: { entityType: entity, includeInactive: true },
            dataType: 'json'
        }).done(function (res) {
            if (!res.success) {
                $body.html(errorHtml(res.message || 'Failed to load data.'));
                return;
            }
            renderGrid(entity, res.data || []);
        }).fail(function (xhr) {
            $body.html(errorHtml('Server error (' + xhr.status + ') while loading data.'));
        });
    }

    function renderGrid(entity, rows) {
        console.log(rows);

        var c = cfg[entity];
        var $body = $('#mdPanelBody');

        var html = '<div class="md-table-wrap"><table class="table md-table">';
        html += '<thead><tr>';
        html += '<th style="width:48px">#</th>';
        //html += '<th>' + c.label + ' Name</th>';
        //if (c.hasParent) html += '<th>' + c.parentLabel + '</th>';
        html += '<th>' + c.label + ' Name</th>';

        if (entity === 'ward') {
            html += '<th>City</th>';
            html += '<th>Ward No</th>';
        }
        else if (entity === 'category') {
            html += '<th>Department</th>';
        }
        else if (c.hasParent) {
            html += '<th>' + c.parentLabel + '</th>';
        }
        html += '<th style="width:90px">Status</th>';
        html += '<th style="width:110px">Actions</th>';
        html += '</tr></thead><tbody>';

        if (rows.length === 0) {
            //var cols = c.hasParent ? 5 : 4;
            var cols = 4;

            if (entity === 'ward')
                cols = 6;   // #, Name, City, Ward No, Status, Actions
            else if (entity === 'category')
                cols = 5;   // #, Name, Department, Status, Actions
            else if (c.hasParent)
                cols = 5;
            html += '<tr><td colspan="' + cols + '" class="md-empty">' +
                '<i class="bi bi-inbox fs-2 d-block mb-2"></i>No ' +
                c.label.toLowerCase() + 's found.</td></tr>';
        } else {
            $.each(rows, function (i, row) {
                var rowId = row.Id !== undefined ? row.Id : row.id;
                var rowName = row.Name || row.name;
                var rowIsActive = row.IsActive !== undefined ? row.IsActive : row.isActive;

                html += '<tr data-id="' + rowId + '">';
                html += '<td>' + (i + 1) + '</td>';
                html += '<td><strong>' + escHtml(rowName) + '</strong></td>';
                //if (c.hasParent) html += '<td>' + escHtml(row.ParentName || row.parentName || '—') + '</td>';
                if (entity === 'ward') {
                    html += '<td>' + escHtml(row.ParentName || '—') + '</td>';
                    html += '<td>' + escHtml(row.WardNo || '—') + '</td>';
                }
                else if (entity === 'category') {
                    html += '<td>' + escHtml(row.DepartmentName || '—') + '</td>';
                }
                else if (c.hasParent) {
                    html += '<td>' + escHtml(row.ParentName || '—') + '</td>';
                }
                html += '<td>' + statusBadge(rowIsActive) + '</td>';

                html += '<td>';

                html += '<button type="button" class="md-action edit me-1" '
                    + 'data-entity="' + entity + '" '
                    + 'data-row=\'' + JSON.stringify(row).replace(/'/g, '&#39;') + '\' '
                    + 'title="Edit"><i class="bi bi-pencil-fill"></i></button>';
                if (row.IsActive) {
                    html += '<button type="button" class="md-action toggle deactivate" '
                        + 'data-entity="' + entity + '" '
                        + 'data-row=\'' + JSON.stringify(row).replace(/'/g, '&#39;') + '\' '
                        + 'title="Deactivate"><i class="bi bi-slash-circle"></i></button>';
                } else {
                    html += '<button type="button" class="md-action toggle" '
                        + 'data-entity="' + entity + '" '
                        + 'data-row=\'' + JSON.stringify(row).replace(/'/g, '&#39;') + '\' '
                        + 'title="Activate"><i class="bi bi-check-circle"></i></button>';
                }
                html += '</td></tr>';
            });
        }

        html += '</tbody></table></div>';
        $body.html(html);

        // Delegate edit / toggle
        $body.off('click.md').on('click.md', '.md-action.edit', function () {
            var row = JSON.parse($(this).attr('data-row'));
            openModal($(this).data('entity'), row);
        }).on('click.md', '.md-action.toggle', function () {
            var $btn = $(this);
            var row = JSON.parse($btn.attr('data-row'));
            var entity = $btn.data('entity');
            $btn.prop('disabled', true);
            var rowIsActive = row.IsActive !== undefined ? row.IsActive : row.isActive;
            var updated = $.extend({}, row, { IsActive: !rowIsActive, isActive: !rowIsActive });
            saveRow(entity, updated,
                function () { loadGrid(entity); },
                function () { $btn.prop('disabled', false); }
            );
        });
    }

    /* ── Add / Edit modal ─────────────────────────────────────── */
    function openModal(entity, row) {
        var c = cfg[entity];
        clearFormError();

        $('#mEntityType').val(entity);
        $('#masterModalTitle').text((row ? 'Edit ' : 'Add ') + c.label);
        $('#mId').val(row ? (row.Id || row.id || 0) : 0);
        $('#mName').val(row ? (row.Name || row.name || '') : '');
        $('#mIsActive').prop('checked', row ? (row.IsActive !== undefined ? row.IsActive : row.isActive) : true);

        if (entity === 'ward') {
            $('#mWardNoGroup').show();
            $('#mWardNo').prop('required', true).val(row ? (row.WardNo || row.wardNo || '') : '');
        } else {
            $('#mWardNoGroup').hide();
            $('#mWardNo').prop('required', false).val('');
        }
        if (entity === 'category') {
            $('#mDepartmentIdGroup').show();
            $('#mDepartmentId').prop('required', true).val(row ? (row.DepartmentId || row.departmentId || '') : '');
            var rowDepartmentId = row ? (row.DepartmentId !== undefined ? row.DepartmentId : row.departmentId) : null;
            // populateParent('department', rowDepartmentId);
            populateParent('department', rowDepartmentId, '#mDepartmentId');
        } else {
            $('#mDepartmentIdGroup').hide();
            $('#mDepartmentId').prop('required', false).val('');
        }
        if (c.hasParent) {
            $('#mParentGroup').show();
            $('#mParentLabel').text(c.parentLabel + ' *');
            var rowParentId = row ? (row.ParentId !== undefined ? row.ParentId : row.parentId) : null;
            // populateParent(c.parentEntity, rowParentId);
            populateParent(c.parentEntity, rowParentId, '#mParentId');
        } else {

            $('#mParentGroup').hide();
            $('#mParentId').empty();
        }

        if (modalInstance) {
            modalInstance.show();
        } else {
            $('#masterModal').modal('show');
        }
    }

    /* function populateParent(parentEntity, selectedId) {
         var $sel = $('#mParentId');
         $sel.prop('disabled', true).empty().append('<option value="">Loading...</option>');
 
         $.ajax({
             url: urls.list,
             type: 'GET',
             data: { entityType: parentEntity, includeInactive: false },
             dataType: 'json'
         }).done(function (res) {
             $sel.empty();
             if (res.success && res.data && res.data.length) {
                 $sel.append('<option value="">-- Select ' + cfg[parentEntity].label + ' --</option>');
                 $.each(res.data, function (i, opt) {
                     $sel.append($('<option>').val(opt.Id).text(opt.Name));
                 });
                 if (selectedId) $sel.val(String(selectedId));
             } else {
                 $sel.append('<option value="">No options available</option>');
             }
         }).fail(function () {
             $sel.empty().append('<option value="">Failed to load</option>');
         }).always(function () {
             $sel.prop('disabled', false);
         });
     }*/
    function populateParent(parentEntity, selectedId, selector) {

        var $sel = $(selector || '#mParentId');

        $sel.prop('disabled', true)
            .empty()
            .append('<option value="">Loading...</option>');

        $.ajax({
            url: urls.list,
            type: 'GET',
            data: {
                entityType: parentEntity,
                includeInactive: false
            },
            dataType: 'json'
        }).done(function (res) {

            $sel.empty();

            if (res.success && res.data.length) {

                $sel.append('<option value="">-- Select ' + cfg[parentEntity].label + ' --</option>');

                $.each(res.data, function (i, item) {
                    $sel.append(
                        $('<option>')
                            .val(item.Id)
                            .text(item.Name)
                    );
                });

                if (selectedId) {
                    $sel.val(selectedId);
                }
            }
        }).always(function () {
            $sel.prop('disabled', false);
        });
    }
    /* ── Save (form submit) ───────────────────────────────────── */
    function submitForm() {
        clearFormError();

        var entity = $('#mEntityType').val();
        var c = cfg[entity];

        if (!$('#mName').val().trim()) {
            showFormError('Name is required.');
            return;
        }
        if (c.hasParent && !$('#mParentId').val()) {
            showFormError('Please select a ' + c.parentLabel + '.');
            return;
        }

        var $btn = $('#btnSave');
        $btn.prop('disabled', true).html('<i class="bi bi-hourglass-split me-1"></i> Saving...');

        // Build payload manually so the checkbox "false" is handled correctly
        var data = {
            Id: $('#mId').val(),
            EntityType: entity,
            Name: $('#mName').val(),
            ParentId: $('#mParentId').val() || '',
            IsActive: $('#mIsActive').is(':checked'),
            WardNo: $('#mWardNo').val(),
            DepartmentId: $('#mDepartmentId').val()
        };
        data.__RequestVerificationToken = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: urls.save,
            type: 'POST',
            data: data,
            dataType: 'json'
        }).done(function (res) {
            if (res.success) {
                if (modalInstance) { modalInstance.hide(); }
                else { $('#masterModal').modal('hide'); }
                loadGrid(entity);
                toast(res.message || 'Saved successfully.', 'success', 'bi-check-circle-fill');
            } else {
                showFormError(res.message || 'Save failed.');
            }
        }).fail(function (xhr) {
            showFormError('Server error (' + xhr.status + ') while saving.');
        }).always(function () {
            $btn.prop('disabled', false).html('<i class="bi bi-check-lg me-1"></i> Save');
        });
    }

    /* ── Inline toggle save ───────────────────────────────────── */
    function saveRow(entity, row, onSuccess, onDone) {
        var data = {
            Id: row.Id !== undefined ? row.Id : row.id,
            EntityType: entity,
            Name: row.Name || row.name,
            ParentId: (row.ParentId !== undefined ? row.ParentId : row.parentId) || '',
            IsActive: row.IsActive !== undefined ? row.IsActive : row.isActive,
            WardNo: row.WardNo || row.wardNo || '',
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        };

        $.ajax({
            url: urls.save,
            type: 'POST',
            data: data,
            dataType: 'json'
        }).done(function (res) {
            if (res.success) {
                toast(res.message || 'Updated.', 'success', 'bi-check-circle-fill');
                if (onSuccess) onSuccess();
            } else {
                toast(res.message || 'Update failed.', 'danger', 'bi-x-circle-fill');
            }
        }).fail(function () {
            toast('Server error.', 'danger', 'bi-x-circle-fill');
        }).always(function () {
            if (onDone) onDone();
        });
    }

    /* ── Helpers ──────────────────────────────────────────────── */
    function statusBadge(isActive) {
        return isActive
            ? '<span class="md-badge-active">Active</span>'
            : '<span class="md-badge-inactive">Inactive</span>';
    }

    function loadingHtml() {
        return '<div class="md-loading"><div class="md-spinner"></div><span>Loading records...</span></div>';
    }

    function errorHtml(msg) {
        return '<div class="alert alert-danger rounded-3 border-0" style="background:rgba(248,113,113,.12);color:#fca5a5;">' +
            '<i class="bi bi-exclamation-triangle-fill me-2"></i>' + escHtml(msg) + '</div>';
    }

    function showFormError(msg) {
        $('#masterFormError').html('<i class="bi bi-exclamation-circle me-1"></i>' + escHtml(msg)).show();
    }

    function clearFormError() {
        $('#masterFormError').text('').hide();
    }

    function toast(msg, type, icon) {
        var $c = $('#mdToastContainer');
        var $t = $('<div class="md-toast ' + type + '">' +
            '<i class="bi ' + icon + '"></i> ' + escHtml(msg) +
            '</div>');
        $c.append($t);
        setTimeout(function () {
            $t.css({ opacity: 0, transition: 'opacity .3s' });
            setTimeout(function () { $t.remove(); }, 350);
        }, 2800);
    }

    function escHtml(str) {
        if (str === null || str === undefined) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    return { init: init };

})(jQuery);
