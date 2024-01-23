export function toBase64(byteArray) {
    let binary = '';
    const len = byteArray.byteLength;

    for (let i = 0; i < len; i++) {
        binary += String.fromCharCode(byteArray[i]);
    }

    return window.btoa(binary);
}


