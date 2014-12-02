$(document).ready(function () {
    setInterval(function () {
        var loc = location.protocol + '//' + location.host + '/exercise?c=Updates&m=Get';
        $.ajax({
            type: 'GET',
            url: loc,
            datatype: 'xml',
            success: function (xml) {
                $(xml).find('update').each(function () {
                    var name = $(this).attr('name');
                    var val = $(this).attr('value');
                    var sel = "span[name='" + name + "']";
                    $(sel).text(val);
                })
            },
        })
    }, 1000);
})