function highlightText(id) {
    console.log('highlight called for ' + id);
    $('.' + id).css('background-color', '#fcc');
}
function removeHighlightText(id) {
    console.log('highlight called for ' + id);
    $('.' + id).css('background-color', '');
}
function show(id, fieldName) {
    $('.fields').css('display', 'none');
    $('#field-' + id).css('display', 'block');
    console.log(fieldName);
}
$('#main-menu .dropdown-item').on("click", function () {
    $('#overlay').css('display', 'flex');
});
$("form").on("submit", function (event) {
    $('#overlay').css('display', 'flex');
});
$('#fieldId').on('change', function () {
    console.log('#field-' + this.value);
    $('.fields').css('display', 'none');
    $('#field-' + this.value).css('display', 'block');

    var selected = $(this).find('option:selected');
    var extra = selected.data('field');
    $('#field').val(extra);
});