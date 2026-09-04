//Shared behavior for admin multi-select popups whose grid is backed by DataTables: the grid re-queries
//the server on every page/search change, which wipes and re-renders every checkbox unchecked - without
//this, only whatever is checked on the CURRENT page at Save time gets submitted, silently dropping
//selections made on pages the admin already navigated away from
function initPersistedCheckboxSelection(options) {
    var $table = $(options.gridSelector);
    var $form = $(options.formSelector);
    var checkboxSelector = options.checkboxSelector || 'input.checkboxGroups';
    var persistedIds = new Set();

    $table.on('change', checkboxSelector, function () {
        var id = $(this).val();
        if ($(this).is(':checked')) {
            persistedIds.add(id);
        } else {
            persistedIds.delete(id);
        }
    });

    //re-apply the checked state for rows the admin already selected whenever this page/search result
    //re-renders them, so a page revisited later still shows what was picked
    $table.on('draw.dt', function () {
        $table.find(checkboxSelector).each(function () {
            if (persistedIds.has($(this).val())) {
                $(this).prop('checked', true);
            }
        });
        updateMasterCheckbox($table.parents('.dt-scroll').first());
    });

    $form.on('submit', function () {
        //hand off submission from the checkboxes (only the current page's) to hidden inputs built
        //from the full persisted set, so nothing already checked on another page gets lost
        $table.find(checkboxSelector + '[name="' + options.hiddenFieldName + '"]').removeAttr('name');
        persistedIds.forEach(function (id) {
            $('<input>').attr({ type: 'hidden', name: options.hiddenFieldName, value: id }).appendTo($form);
        });
    });
}
