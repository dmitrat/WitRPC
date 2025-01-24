window.cryptoInterop = {
    privateKey: null,
    publicKey: null,

    async generateKeys(keySize) {
        const keyPair = await window.crypto.subtle.generateKey(
            {
                name: "RSA-OAEP",
                modulusLength: keySize,
                publicExponent: new Uint8Array([1, 0, 1]),
                hash: "SHA-256"
            },
            true,
            ["encrypt", "decrypt"]
        );

        this.privateKey = keyPair.privateKey;
        this.publicKey = keyPair.publicKey;
    },

    async getPublicKey() {
        const exported = await window.crypto.subtle.exportKey("jwk", this.publicKey);
        return JSON.stringify(exported);
    },

    async getPrivateKey() {
        const exported = await window.crypto.subtle.exportKey("jwk", this.privateKey);
        return JSON.stringify(exported);
    },

    async decryptRSA(encryptedBase64) {
        const encryptedData = Uint8Array.from(atob(encryptedBase64), c => c.charCodeAt(0)).buffer;

        const decrypted = await window.crypto.subtle.decrypt(
            {
                name: "RSA-OAEP"
            },
            this.privateKey,
            encryptedData
        );

        const decryptedBase64 = btoa(String.fromCharCode(...new Uint8Array(decrypted)));
        return decryptedBase64;
    },

    async encryptAes(base64Key, base64Iv, base64Data) {
        const keyBytes = Uint8Array.from(atob(base64Key), c => c.charCodeAt(0));
        const ivBytes = Uint8Array.from(atob(base64Iv), c => c.charCodeAt(0));
        const dataBytes = Uint8Array.from(atob(base64Data), c => c.charCodeAt(0));

        const key = await window.crypto.subtle.importKey(
            "raw",
            keyBytes,
            {
                name: "AES-CBC"
            },
            false,
            ["encrypt"]
        );

        const encrypted = await window.crypto.subtle.encrypt(
            {
                name: "AES-CBC",
                iv: ivBytes
            },
            key,
            dataBytes
        );

        return btoa(String.fromCharCode(...new Uint8Array(encrypted))); 
    },

    async decryptAes(base64Key, base64Iv, base64EncryptedData) {
        const keyBytes = Uint8Array.from(atob(base64Key), c => c.charCodeAt(0));
        const ivBytes = Uint8Array.from(atob(base64Iv), c => c.charCodeAt(0));
        const encryptedBytes = Uint8Array.from(atob(base64EncryptedData), c => c.charCodeAt(0));

        const key = await window.crypto.subtle.importKey(
            "raw",
            keyBytes,
            {
                name: "AES-CBC"
            },
            false,
            ["decrypt"]
        );

        const decrypted = await window.crypto.subtle.decrypt(
            {
                name: "AES-CBC",
                iv: ivBytes
            },
            key,
            encryptedBytes
        );

        return btoa(String.fromCharCode(...new Uint8Array(decrypted))); 
    }
};
