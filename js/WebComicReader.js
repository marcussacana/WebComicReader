var Width = $(window).width();
var Height = $(window).height();

function RefreshSize() {
    Width = $(window).width();
    Height = $(window).height();

    if (typeof (localStorage['width']) !== 'undefined') {
        Width = parseInt(localStorage['width']);
    }
    if (typeof (localStorage['height']) !== 'undefined') {
        Height = parseInt(localStorage['height']);
    }

    if (typeof (localStorage['diffwidth']) !== 'undefined') {
        Width += parseInt(localStorage['diffwidth']);
    }
}

RefreshSize();

async function SetWidthDiff(Diff) {
    localStorage['diffwidth'] = Diff;
}

async function LoadComic() {
    await sleep(500);

    RefreshSize();

    ResizeDoubles();

    $('.flipbook').turn({
        width: Width,
        height: Height,
        elevation: 50,
        gradients: true,
        autoCenter: true
    });

    PostLoad(false);

    return Width / 2;
}

async function LoadManga() {
    await sleep(500);

    RefreshSize();

    ResizeDoubles(undefined, true);

    $('.flipbook').turn({
        width: Width,
        height: Height,
        elevation: 50,
        gradients: true,
        autoCenter: true
    });

    PostLoad(true);

    return Width / 2;
}

function ResizeDoubles(InZoom, Reverse) {
    let SecondaryClass = Reverse ? "even" : "odd";
    let width = Width;
    let height = Height;
    let pageWidth = Width / 2;
    let zoomWidth = largeWidth();
    let zoomHeight = largeHeight();
    let zoomPageWidth = largeWidth() / 2;

    $('.flipbook .double').each(function (i) {
        if (InZoom) {
            //Zoom Enter
            this.style.width = this.parentElement.style.width = `${zoomWidth}px`;
            this.style.height = this.parentElement.style.height = `${zoomHeight}px`;
            if ($(this.parentElement).hasClass(SecondaryClass))
                this.style.marginLeft = `${zoomPageWidth * -1}px`;

        } else if (typeof (InZoom) == 'undefined') {
            //Book Initialize
            this.style.width = `${width}px`;
            this.style.height = `${height}px`;
            Reverse ? $(this).scissorReverse() : $(this).scissor();
        } else {
            //Zoom Exit
            this.style.width = this.parentElement.style.width = `${width}px`;
            this.style.height = this.parentElement.style.height = `${height}px`;
            if ($(this.parentElement).hasClass(SecondaryClass))
                this.style.marginLeft = `${pageWidth * -1}px`;
        }
    });
}

function PostLoad(swap) {
    $('.flipbook-viewport').zoom({
        flipbook: $('.flipbook'),

        max: function () {
            return largeWidth() / $('.flipbook').width();
        },

        when: {
            swipeLeft: function () {

                $(this).zoom('flipbook').turn('next');

            },

            swipeRight: function () {

                $(this).zoom('flipbook').turn('previous');

            },

            resize: function (event, scale, page, pageElement) {
                ResizeDoubles(scale != 1, swap);
            },

            zoomIn: function () {

                $('#slider-bar').hide();
                $('.made').hide();
                $('.flipbook').removeClass('animated').addClass('zoom-in');

                if (!window.escTip && !$.isTouch) {
                    escTip = true;

                    $('<div />', { 'class': 'exit-message' }).
                        html('<div>Press ESC to exit</div>').
                        appendTo($('body')).
                        delay(2000).
                        animate({ opacity: 0 }, 500, function () {
                            $(this).remove();
                        });
                }
            },

            zoomOut: function () {

                $('#slider-bar').fadeIn();
                $('.exit-message').hide();
                $('.made').fadeIn();

                setTimeout(function () {
                    $('.flipbook').addClass('animated').removeClass('zoom-in');
                    resizeViewport();
                }, 0);

            }
        }
    });

    if ($.isTouch)
        $('.flipbook-viewport').bind('zoom.doubleTap', zoomTo);
    else
        $('.flipbook-viewport').bind('zoom.tap', zoomTo);

    $(document).keydown(function (e) {
        var previous = 37, next = 39, esc = 27;

        if (swap)
            previous = 39, next = 37;

        switch (e.keyCode) {
            case previous:
                $('.flipbook').turn('previous');
                e.preventDefault();
                break;
            case next:
                $('.flipbook').turn('next');
                e.preventDefault();
                break;
            case esc:
                $('.flipbook-viewport').zoom('zoomOut');
                e.preventDefault();
                break;
        }
    });
    $('.flipbook-viewport')[0].style.position = "unset";
}

function zoomTo(event) {

    setTimeout(function () {
        if ($('.flipbook-viewport').zoom('value') == 1) {
            $('.flipbook-viewport').zoom('zoomIn', event);
        } else {
            $('.flipbook-viewport').zoom('zoomOut');
        }
    }, 1);

}

function resizeViewport() {

    var width = Width,
        height = Height,
        options = $('.flipbook').turn('options');

    $('.flipbook').removeClass('animated');

    $('.flipbook-viewport').css({
        width: width,
        height: height
    }).zoom('resize');

    var flipbookOffset = $('.flipbook').offset(),
        boundH = height - flipbookOffset.top - $('.flipbook').height(),
        marginTop = (boundH - $('.thumbnails > div').height()) / 2;

    if (marginTop < 0) {
        $('.thumbnails').css({ height: 1 });
    } else {
        $('.thumbnails').css({ height: boundH });
        $('.thumbnails > div').css({ marginTop: marginTop });
    }

    if (flipbookOffset.top < $('.made').height())
        $('.made').hide();
    else
        $('.made').show();

    $('.flipbook').addClass('animated');

}

function largeWidth() {
    var width = Width;
    return width + (width * 0.50);//50% of zoom
}

function largeHeight() {
    var height = Height;
    return height + (height * 0.50);//50% of zoom
}

async function OpenFile() {
    comic.click();
}

async function Fullscreen() {
    if (document.documentElement.requestFullscreen) {
        await document.documentElement.requestFullscreen();
    } else if (document.documentElement.msRequestFullscreen) {
        await document.documentElement.msRequestFullscreen();
    } else if (document.documentElement.mozRequestFullScreen) {
        await document.documentElement.mozRequestFullScreen();
    } else if (document.documentElement.webkitRequestFullscreen) {
        await document.documentElement.webkitRequestFullscreen(Element.ALLOW_KEYBOARD_INPUT);
    }
}

async function b64toBlobUrl(b64, type) {
    return window.URL.createObjectURL(await b64toBlob(b64, type));
}

async function b64toBlob(base64, type = 'application/octet-stream') {
    var Res = await fetch(`data:${type};base64,${base64}`);
    return Res.blob();
}

async function getContentLength(URL) {
    var Resp = await fetch(URL, {
        "body": null,
        "method": "HEAD",
        "mode": "no-cors"
    });
    return Resp.headers.get("Content-Length");
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function getBaseDirectory() {
    var Elm = document.getElementsByTagName("base")[0];
    return Elm.getAttribute("href");
}

function getHash() {
    return location.hash.substr(1);
}

function AspectRatio(w, h) {

    if (w == null)
        w = $(window).width();
    if (h == null)
        h = $(window).height();

    var aspectRatio, dividend, divisor;
    if (h == w) {
        aspectRatio = '1:1';
    } else {
        if (h > w) {
            dividend = h;
            divisor = w;
        }

        if (w > h) {
            dividend = w;
            divisor = h;
        }

        var gcd = -1;
        while (gcd == -1) {
            remainder = dividend % divisor;
            if (remainder == 0) {
                gcd = divisor;
            } else {
                dividend = divisor;
                divisor = remainder;
            }
        }

        var hr = w / gcd;
        var vr = h / gcd;
        aspectRatio = (hr + ':' + vr);

    }

    return aspectRatio;
}