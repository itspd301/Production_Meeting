// Shared behavior for the Add/Edit User plant-access picker (see _UserForm.cshtml).
function initUserAccessForm(getLinesUrl) {
    var dataEl = document.getElementById('pmUserFormData');
    var accessRows = dataEl ? JSON.parse(dataEl.textContent || '[]') : [];

    function renderAccessRows() {
        var $body = $('#accessTable tbody').empty();
        var $inputs = $('#accessInputs').empty();

        accessRows.forEach(function (row, i) {
            $body.append(
                '<tr>' +
                    '<td>' + row.plantName + '</td>' +
                    '<td>' + (row.lineName || 'All Lines') + '</td>' +
                    '<td><button type="button" class="btn btn-sm btn-outline-danger pm-remove-access" data-index="' + i + '"><i class="bi bi-x-lg"></i></button></td>' +
                '</tr>'
            );
            $inputs.append('<input type="hidden" name="PlantAccess[' + i + '].PlantId" value="' + row.plantId + '" />');
            if (row.lineId) {
                $inputs.append('<input type="hidden" name="PlantAccess[' + i + '].LineId" value="' + row.lineId + '" />');
            }
        });

        $('.pm-remove-access').on('click', function () {
            accessRows.splice(parseInt($(this).data('index'), 10), 1);
            renderAccessRows();
        });
    }

    $('#accessPlantSelect').on('change', function () {
        var plantId = $(this).val();
        var $line = $('#accessLineSelect');
        $line.prop('disabled', true).html('<option value="">All Lines</option>');

        if (!plantId) return;

        $.get(getLinesUrl, { plantId: plantId }, function (data) {
            data.forEach(function (l) {
                $line.append('<option value="' + l.lineId + '">' + l.name + '</option>');
            });
            $line.prop('disabled', false);
        });
    });

    $('#btnAddAccess').on('click', function () {
        var plantId = $('#accessPlantSelect').val();
        if (!plantId) {
            toastr.warning('Select a plant first.');
            return;
        }
        var plantName = $('#accessPlantSelect option:selected').text();
        var lineId = $('#accessLineSelect').val() || null;
        var lineName = lineId ? $('#accessLineSelect option:selected').text() : null;

        if (accessRows.some(function (r) { return r.plantId == plantId && r.lineId == lineId; })) {
            toastr.warning('This access entry already exists.');
            return;
        }

        accessRows.push({ plantId: parseInt(plantId, 10), plantName: plantName, lineId: lineId ? parseInt(lineId, 10) : null, lineName: lineName });
        renderAccessRows();
    });

    renderAccessRows();
}
