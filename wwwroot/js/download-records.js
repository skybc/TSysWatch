// 初始化下载记录功能
function initializeDownloadRecords(moduleName, apiPrefix) {
    $(function() {
        const startDateInput = $(`#${moduleName}StartDate`);
        const endDateInput = $(`#${moduleName}EndDate`);
        
        // 设置日期默认值为今天
        const today = new Date().toISOString().split('T')[0];
        startDateInput.val(today);
        endDateInput.val(today);

        $(`#${moduleName}DownloadBtn`).on('click', function() {
            const modal = new bootstrap.Modal(document.getElementById(`${moduleName}DownloadModal`));
            modal.show();
        });

        $(`#${moduleName}SearchBtn`).on('click', function() {
            const startDate = startDateInput.val();
            const endDate = endDateInput.val();
            
            $.get(`/${apiPrefix}/Get${moduleName}RecordFiles`, {
                startDate: startDate,
                endDate: endDate
            }, function(data) {
                if (data.success && data.files.length > 0) {
                    let html = '<table class="table table-sm"><thead><tr><th>文件名</th><th>大小</th><th>日期</th></tr></thead><tbody>';
                    data.files.fovrEach(file => {
                        const size = (file.size / 1024).toFixed(2) + ' KB';
                        const date = new Date(file.created).toLocaleString('zh-CN');
                        html += `<tr><td>${file.name}</td><td>${size}</td><td>${date}</td></tr>`;
                    });
                    html += '</tbody></table>';
                    $(`#${moduleName}FilesTable`).html(html);
                    $(`#${moduleName}FilesList`).show();
                } else {
                    alert('未找到文件');
                    $(`#${moduleName}FilesList`).hide();
                }
            });
        });

        $(`#${moduleName}ExportBtn`).on('click', function() {
            const startDate = startDateInput.val();
            const endDate = endDateInput.val();
            window.location.href = `/${apiPrefix}/Download${moduleName}Records?startDate=${startDate}&endDate=${endDate}`;
        });
    });
}
