export const printingInterop = {
    isSmartphone: function () {
        const ua = navigator.userAgent || navigator.vendor || window.opera;
        return /android/i.test(ua) || /iPhone|iPad|iPod/i.test(ua) || /Mobile|Tablet|Phone/i.test(ua);
    },
    base64ToBlob: function (base64, mime) {
        const byteChars = atob(base64);
        const byteNumbers = new Array(byteChars.length);
        for (let i = 0; i < byteChars.length; i++) {
            {
                byteNumbers[i] = byteChars.charCodeAt(i);
            }
        }
        const byteArray = new Uint8Array(byteNumbers);
        return new Blob([byteArray], { type: mime });
    },
    print: function (base64Data, mime, title) {
        const blob = this.base64ToBlob(base64Data, mime);
        const blobUrl = URL.createObjectURL(blob);

        if (this.isSmartphone()) {

            // On smartphones → open PDF in viewer
            var opener = window.open(blobUrl, '_blank');
            opener.print();
            setInterval(() => {
                {
                    if (opener.hasFocus()) {
                        opener.close();
                    }
                }
            }, 1000);
        }
        else {
            // Create a hidden iframe
            const iframe = document.createElement("iframe");
            iframe.src = blobUrl;
            document.body.appendChild(iframe);

            iframe.onload = () => {
                {

                    //iframe.contentDocument.title = title;
                    // Wait a tiny bit for the PDF viewer to render
                    setTimeout(() => {
                        {
                            iframe.contentWindow.focus();
                            iframe.contentWindow.print();
                        }
                    }, 500);
                }
            };
        }
    }
};