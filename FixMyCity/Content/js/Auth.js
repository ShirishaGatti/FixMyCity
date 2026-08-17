$(document).ready(function () {
    // Password toggle
    $('#togglePassword').on('click', function () {
        var pwdInput = $('#Password, input[name="Password"]');
        var eyeIcon = $('#eyeIcon');
        if (pwdInput.attr('type') === 'password') {
            pwdInput.attr('type', 'text');
            eyeIcon.removeClass('bi-eye-fill').addClass('bi-eye-slash-fill');
        } else {
            pwdInput.attr('type', 'password');
            eyeIcon.removeClass('bi-eye-slash-fill').addClass('bi-eye-fill');
        }
    });
    $('#otpForm').on('submit', function (e) {
        e.preventDefault();
        var form = $(this);
        var submitBtn = $('#otpSubmit');
        var spinner = $('#otpSpinner');

        submitBtn.prop('disabled', true);
        spinner.removeClass('d-none');

        $.ajax({
            url: form.attr('action'),
            type: 'POST',
            data: form.serialize(),
            success: function (res) {
                submitBtn.prop('disabled', false);
                spinner.addClass('d-none');

                if (res.success) {
                    window.location.href = res.redirectUrl || '/Citizen/MyComplaints';
                } else {
                    showAlert('#alertContainer', res.message || 'Invalid or expired OTP.', 'danger');
                }
            },
            error: function (xhr) {
                submitBtn.prop('disabled', false);
                spinner.addClass('d-none');
                var msg = 'An error occurred while verifying OTP. Please try again.';
                if (xhr.responseJSON && xhr.responseJSON.message) msg = xhr.responseJSON.message;
                showAlert('#alertContainer', msg, 'danger');
            }
        });
    });
    // Dynamic Ward Dropdown population on City change
    $('#CityId').on('change', function () {
        var cityId = $(this).val();
        var wardSelect = $('#WardId');
        wardSelect.empty().append('<option value="">-- Select Ward --</option>');

        if (cityId) {
            $.ajax({
                url: '/Account/GetWards',
                type: 'GET',
                data: { cityId: cityId },
                success: function (data) {
                    $.each(data, function (index, item) {
                        wardSelect.append($('<option></option>').val(item.id).text(item.name));
                    });
                }
            });
        }
    });

    // Helper: Show Alert
    function showAlert(containerId, message, type) {
        var container = $(containerId);
        if (!container.length) {
            container = $('<div id="alertContainer" class="mb-3"></div>').insertBefore('form');
        }
        var icon = type === 'success' ? 'bi-check-circle-fill' : 'bi-exclamation-triangle-fill';
        var alertHtml = '<div class="alert alert-' + type + ' alert-dismissible fade show shadow-sm" role="alert">' +
            '<i class="bi ' + icon + ' me-2"></i>' + message +
            '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>' +
            '</div>';
        container.html(alertHtml);
    }

    // Login Form Submit
    $('#loginForm').on('submit', function (e) {
        e.preventDefault();
        var form = $(this);
        var submitBtn = $('#loginSubmit');
        var spinner = $('#loginSpinner');

        submitBtn.prop('disabled', true);
        spinner.removeClass('d-none');

        $.ajax({
            url: form.attr('action'),
            type: 'POST',
            data: form.serialize(),
            success: function (res) {
                submitBtn.prop('disabled', false);
                spinner.addClass('d-none');

                if (res.success) {
                    showAlert('#alertContainer', 'Authentication successful! Redirecting...', 'success');
                    setTimeout(function () {
                        window.location.href = res.redirectUrl || '/Account/VerifyOtp';
                    }, 600);
                } else {
                    showAlert('#alertContainer', res.message || 'Invalid email or password.', 'danger');
                }
            },
            error: function (xhr) {
                submitBtn.prop('disabled', false);
                spinner.addClass('d-none');
                var msg = 'An error occurred while attempting to log in. Please try again.';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    msg = xhr.responseJSON.message;
                }
                showAlert('#alertContainer', msg, 'danger');
            }
        });
    });

    // Register Form Submit
    $('#registerForm').on('submit', function (e) {
        e.preventDefault();
        var form = $(this);
        var submitBtn = $('#registerSubmit');
        var spinner = $('#registerSpinner');

        submitBtn.prop('disabled', true);
        spinner.removeClass('d-none');

        $.ajax({
            url: form.attr('action'),
            type: 'POST',
            data: form.serialize(),
            success: function (res) {
                submitBtn.prop('disabled', false);
                spinner.addClass('d-none');

                if (res.success) {
                    showAlert('#alertContainer', res.message || 'Registration successful! Redirecting to login...', 'success');
                    setTimeout(function () {
                        window.location.href = res.redirectUrl || '/Account/Login';
                    }, 1200);
                } else {
                    showAlert('#alertContainer', res.message || 'Registration failed. Please check your inputs.', 'danger');
                }
            },
            error: function (xhr) {
                submitBtn.prop('disabled', false);
                spinner.addClass('d-none');
                var msg = 'Registration failed. Please try again.';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    msg = xhr.responseJSON.message;
                }
                showAlert('#alertContainer', msg, 'danger');
            }
        });
    });
});
