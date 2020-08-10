async function LoadComic() {
    await sleep(500);
    $('.flipbook .double').each(function (i) {
        this.style.width = `${$(window).width()}px`;
        this.style.height = `${$(window).height()}px`;
        $(this).scissor();
    });

    $('.flipbook').turn({
        width: $(window).width(),
        height: $(window).height(),
        elevation: 50,
        gradients: true,
        autoCenter: true
    });
    return $(window).width() / 2;
}

async function LoadManga() {
    await sleep(500);
    $('.flipbook .double').each(function (i) {
        this.style.width = `${$(window).width()}px`;
        this.style.height = `${$(window).height()}px`;
        $(this).scissorReverse();
    });

    $('.flipbook').turn({
        width: $(window).width(),
        height: $(window).height(),
        elevation: 50,
        gradients: true,
        autoCenter: true
    });
    return $(window).width() / 2;
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

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function getBaseDirectory() {
    var Elm = document.getElementsByTagName("base")[0];
    return Elm.getAttribute("href");
}