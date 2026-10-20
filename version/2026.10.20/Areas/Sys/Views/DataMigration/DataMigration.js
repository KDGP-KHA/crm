/* CenIT TOC CRM - Data Migration Tool Scripts */

$(function () {
    var DataMigration = {
        init: function () {
            this.bindEvents();
        },

        bindEvents: function () {
            var self = this;

            // Source Type radio toggle
            $('input[name="SourceType"]').on('change', function () {
                var val = $(this).val();
                self.resetPreview();
                if (val === "1") {
                    $('#SourceIdLabel').text('ID Cơ hội');
                    $('#SourceId').attr('placeholder', 'Nhập ID cơ hội (ví dụ: 166)');
                    $('#SourceIdHelp').text('ID trong URL: /Cate/BusinessOpportunityOverview/Index/{ID}');
                    $('#ChecklistInput').val('UNCAPTURED; APPROACHING');
                } else {
                    $('#SourceIdLabel').text('ID Dự án');
                    $('#SourceId').attr('placeholder', 'Nhập ID dự án (ví dụ: 42)');
                    $('#SourceIdHelp').text('ID trong URL: /Cate/ProjectOverview/Index/{ID}');
                    $('#ChecklistInput').val('UNCAPTURED; APPROACHING; FORMATION; IMPLEMENTING');
                }
            });

            // Preset pills click
            $('.preset-pill').on('click', function () {
                var preset = $(this).data('preset');
                $('#ChecklistInput').val(preset);
            });

            // Status tag click (append to status transition codes using semicolon)
            $('.status-tag').on('click', function () {
                var code = $(this).data('code');
                if (!code) return;
                var current = $('#ChecklistInput').val().trim();
                if (current.length === 0) {
                    $('#ChecklistInput').val(code);
                } else {
                    var parts = current.split(';')
                        .map(function (s) { return s.trim(); })
                        .filter(function (s) { return s.length > 0; });
                    if (parts.indexOf(code) === -1) {
                        parts.push(code);
                        $('#ChecklistInput').val(parts.join('; '));
                    }
                }
            });

            // Clear checklist button
            $('#BtnClearChecklist').on('click', function () {
                $('#ChecklistInput').val('');
            });

            // Preview button
            $('#BtnPreview').on('click', function () {
                self.doPreview();
            });

            // Enter key on SourceId
            $('#SourceId').on('keypress', function (e) {
                if (e.which === 13) {
                    e.preventDefault();
                    self.doPreview();
                }
            });

            // Execute button
            $('#BtnExecute').on('click', function () {
                self.doExecute();
            });

            // Refresh recent button
            $('#BtnRefreshRecent').on('click', function () {
                self.refreshRecent();
            });
        },

        resetPreview: function () {
            $('#PreviewContent').hide();
            $('#PreviewEmpty').show();
            $('#ResultContainer').hide().empty();
            $('#BtnExecute').prop('disabled', true);
        },

        doPreview: function () {
            var self = this;
            var sourceType = parseInt($('input[name="SourceType"]:checked').val(), 10) || 1;
            var sourceId = parseInt($('#SourceId').val(), 10) || 0;

            if (sourceId <= 0) {
                if (typeof toastr !== 'undefined') {
                    toastr.warning('Vui lòng nhập ID hợp lệ (> 0).');
                } else {
                    alert('Vui lòng nhập ID hợp lệ (> 0).');
                }
                $('#SourceId').focus();
                return;
            }

            self.showLoading(true, 'Đang phân tích và tải dữ liệu nguồn...');

            $.ajax({
                url: '/Sys/DataMigration/Preview',
                type: 'POST',
                data: {
                    sourceType: sourceType,
                    sourceId: sourceId
                },
                success: function (res) {
                    self.showLoading(false);
                    if (res && res.status) {
                        self.renderPreview(res.data);
                    } else {
                        self.resetPreview();
                        var msg = (res && res.message) ? res.message : 'Không tìm thấy dữ liệu nguồn.';
                        if (typeof toastr !== 'undefined') {
                            toastr.error(msg);
                        } else {
                            alert(msg);
                        }
                    }
                },
                error: function (xhr, status, err) {
                    self.showLoading(false);
                    self.resetPreview();
                    var errMsg = 'Lỗi kết nối máy chủ khi xem trước: ' + (err || status);
                    if (typeof toastr !== 'undefined') {
                        toastr.error(errMsg);
                    } else {
                        alert(errMsg);
                    }
                }
            });
        },

        renderPreview: function (data) {
            $('#PreviewEmpty').hide();
            $('#PreviewContent').show();
            $('#ResultContainer').hide().empty();

            $('#PrevName').text(data.Title || data.SourceName || '(Không có tên)');
            $('#PrevCode').text(data.SourceCode || '---');
            $('#PrevCustomer').text(data.CustomerName || '(Chưa xác định)');
            $('#PrevAM').text(data.AMName || '(Chưa gán AM)');
            $('#PrevStatus').text(data.CurrentStatusName || '---');

            $('#StatMembers').text(data.MemberCount !== undefined ? data.MemberCount : 0);
            $('#StatProducts').text(data.ProductCount !== undefined ? data.ProductCount : 0);
            $('#StatDiscussions').text(data.ActivityCount !== undefined ? data.ActivityCount : 0);
            $('#StatFiles').text(data.AttachmentCount !== undefined ? data.AttachmentCount : 0);

            // Suggested checklist if input is empty
            var currentChecklist = $('#ChecklistInput').val().trim();
            if (currentChecklist.length === 0 && data.SuggestedChecklist) {
                $('#ChecklistInput').val(data.SuggestedChecklist);
            }

            // Already converted warning
            if (data.AlreadyConverted) {
                $('#PrevWarningConverted').show();
                $('#PrevConvertedLink').attr('href', '/Cate/DigitalSales/Detail/' + data.ExistingDigitalSalesID)
                    .text('Mã SPDV: ' + (data.ExistingDigitalSalesCode || ('ID ' + data.ExistingDigitalSalesID)));
            } else {
                $('#PrevWarningConverted').hide();
            }

            $('#BtnExecute').prop('disabled', false);
        },

        doExecute: function () {
            var self = this;
            var sourceType = parseInt($('input[name="SourceType"]:checked').val(), 10) || 1;
            var sourceId = parseInt($('#SourceId').val(), 10) || 0;
            var checklist = $('#ChecklistInput').val().trim();

            if (sourceId <= 0) {
                toastr.warning('Vui lòng nhập ID hợp lệ (> 0).');
                return;
            }

            if (checklist.length === 0) {
                toastr.warning('Vui lòng nhập danh sách mã trạng thái chuyển đổi (nối nhau bằng dấu ;).');
                $('#ChecklistInput').focus();
                return;
            }

            var typeName = sourceType === 1 ? 'CƠ HỘI' : 'DỰ ÁN';
            var confirmMsg = 'Xác nhận chuyển đổi ' + typeName + ' (ID: ' + sourceId + ') sang hồ sơ Kinh doanh SPDV Số?\n\n' +
                '• Khởi tạo Lịch sử Chuyển trạng thái: ' + checklist + '\n' +
                '• Hệ thống sẽ tự động ánh xạ vai trò, timeline, trao đổi và di chuyển tệp tin đính kèm.';

            if (!confirm(confirmMsg)) {
                return;
            }

            self.showLoading(true, 'Đang tiến hành chuyển đổi dữ liệu, di chuyển tệp tin và cập nhật timeline...');
            $('#BtnExecute').prop('disabled', true);

            $.ajax({
                url: '/Sys/DataMigration/Execute',
                type: 'POST',
                data: {
                    SourceType: sourceType,
                    SourceID: sourceId,
                    StatusTransitionCodes: checklist,
                    MoveAttachments: true
                },
                success: function (res) {
                    self.showLoading(false);
                    $('#BtnExecute').prop('disabled', false);

                    if (res && res.status) {
                        if (typeof toastr !== 'undefined') {
                            toastr.success('Chuyển đổi dữ liệu thành công!');
                        }
                        self.renderResult(res.data);
                        self.refreshRecent();
                    } else {
                        var errMsg = (res && res.message) ? res.message : 'Chuyển đổi thất bại.';
                        if (typeof toastr !== 'undefined') {
                            toastr.error(errMsg);
                        } else {
                            alert(errMsg);
                        }
                    }
                },
                error: function (xhr, status, err) {
                    self.showLoading(false);
                    $('#BtnExecute').prop('disabled', false);
                    var errMsg = 'Lỗi kết nối máy chủ trong quá trình chuyển đổi: ' + (err || status);
                    if (typeof toastr !== 'undefined') {
                        toastr.error(errMsg);
                    } else {
                        alert(errMsg);
                    }
                }
            });
        },

        renderResult: function (data) {
            var html = '<div class="result-success-box animate__animated animate__fadeIn">' +
                '<div class="result-success-header">' +
                '<i class="fa fa-check-circle"></i>' +
                '<span>' + (data.Message || 'Chuyển đổi dữ liệu thành công!') + '</span>' +
                '</div>' +
                '<div class="text-dark-m1 mb-2">' +
                'Hồ sơ SPDV Số mới đã được tạo với mã: <strong class="text-primary-d2 text-110">' + (data.NewDigitalSalesCode || '---') + '</strong>' +
                '</div>' +
                '<div class="row g-2 text-90 my-2">' +
                '<div class="col-6 col-md-3"><strong>Thành viên:</strong> ' + (data.MigratedMembers || 0) + '</div>' +
                '<div class="col-6 col-md-3"><strong>Sản phẩm:</strong> ' + (data.MigratedProducts || 0) + '</div>' +
                '<div class="col-6 col-md-3"><strong>Trao đổi:</strong> ' + (data.MigratedDiscussions || 0) + '</div>' +
                '<div class="col-6 col-md-3"><strong>Tệp tin:</strong> ' + (data.MigratedAttachments || 0) + '</div>' +
                '<div class="col-6 col-md-3"><strong>Milestones:</strong> ' + (data.TimelineMilestones || 0) + '</div>' +
                '<div class="col-6 col-md-3"><strong>Tracking:</strong> ' + (data.TrackingProcesses || 0) + '</div>' +
                '</div>';

            if (data.Warnings && data.Warnings.length > 0) {
                html += '<div class="migration-warnings">' +
                    '<strong><i class="fa fa-exclamation-triangle mr-1"></i> Lưu ý / Cảnh báo:</strong><ul class="mb-0 pl-3">';
                for (var i = 0; i < data.Warnings.length; i++) {
                    html += '<li>' + data.Warnings[i] + '</li>';
                }
                html += '</ul></div>';
            }

            html += '<div class="mt-3">' +
                '<a href="/Cate/DigitalSales/Detail/' + data.NewDigitalSalesID + '" target="_blank" class="result-detail-btn">' +
                '<i class="fa fa-external-link-alt mr-2"></i> Mở hồ sơ SPDV Số (' + (data.NewDigitalSalesCode || '') + ')' +
                '</a>' +
                '</div>' +
                '</div>';

            $('#ResultContainer').html(html).show();
        },

        refreshRecent: function () {
            $.ajax({
                url: '/Sys/DataMigration/GetRecent',
                type: 'GET',
                success: function (res) {
                    if (res && res.status && res.data) {
                        var tbody = $('#DSRecentMigrations tbody');
                        tbody.empty();
                        if (res.data.length === 0) {
                            tbody.append('<tr><td colspan="8" class="text-center text-muted py-3">Chưa có dữ liệu chuyển đổi gần đây.</td></tr>');
                            return;
                        }

                        for (var i = 0; i < res.data.length; i++) {
                            var item = res.data[i];
                            var sourceBadge = item.SourceType === 1
                                ? '<span class="badge badge-warning text-dark"><i class="fa fa-lightbulb mr-1"></i>Cơ hội</span>'
                                : '<span class="badge badge-primary"><i class="fa fa-project-diagram mr-1"></i>Dự án</span>';

                            var row = '<tr>' +
                                '<td class="text-center">' + (i + 1) + '</td>' +
                                '<td><strong class="text-primary-d2">' + (item.DigitalSalesCode || '') + '</strong></td>' +
                                '<td class="text-center">' + sourceBadge + '</td>' +
                                '<td class="text-center">' + (item.SourceId || '') + '</td>' +
                                '<td>' + (item.DigitalSalesName || '') + '</td>' +
                                '<td>' + (item.CustomerName || '') + '</td>' +
                                '<td class="text-center text-muted text-90">' + (item.CreatedDateStr || '') + '</td>' +
                                '<td class="text-center text-90">' + (item.CreatedBy || '') + '</td>' +
                                '<td class="text-center">' +
                                '<a href="/Cate/DigitalSales/Detail/' + item.DigitalSalesID + '" target="_blank" class="btn btn-xs btn-outline-primary radius-2" title="Xem chi tiết">' +
                                '<i class="fa fa-eye mr-1"></i>Xem' +
                                '</a>' +
                                '</td>' +
                                '</tr>';
                            tbody.append(row);
                        }
                    }
                }
            });
        },

        showLoading: function (show, text) {
            if (show) {
                $('#LoadingOverlayText').text(text || 'Đang xử lý...');
                $('#LoadingOverlay').addClass('active');
            } else {
                $('#LoadingOverlay').removeClass('active');
            }
        }
    };

    DataMigration.init();
});
