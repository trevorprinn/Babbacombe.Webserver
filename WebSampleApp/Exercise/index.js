$(document).ready(function () {
    setInterval(function () {
        var loc = location.protocol + '//' + location.host + '/exercise?c=Updates&m=Get';
        $.ajax({
            type: 'GET',
            url: loc,
            datatype: 'xml',
            success: function (xml) {
                $(xml).find('update').each(function () {
                    var id = $(this).attr('id');
                    var val = $(this).attr('value');
                    $('#' + id).text(val);
                })
            },
        })
    }, 1000);
})