// WitRPC Crypto Interop for Blazor WebAssembly
// Provides RSA key generation and AES encryption using Web Crypto API

window.cryptoInterop = {
    privateKey: null,
    publicKey: null,

    _uint8ToBase64(bytes) {
        const CHUNK = 0x8000;
        let binary = '';
        for (let i = 0; i < bytes.length; i += CHUNK) {
            binary += String.fromCharCode.apply(null, bytes.subarray(i, i + CHUNK));
        }
        return btoa(binary);
    },

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

        return this._uint8ToBase64(new Uint8Array(decrypted));
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

        return this._uint8ToBase64(new Uint8Array(encrypted));
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

        return this._uint8ToBase64(new Uint8Array(decrypted));
    }
};
