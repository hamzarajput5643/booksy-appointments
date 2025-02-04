const ENCRYPTION_KEY = import.meta.env.VITE_ENCRYPTION_KEY;

if (!ENCRYPTION_KEY) {
    throw new Error('Missing encryption key in environment variables');
}

// Helper function to convert string to ArrayBuffer
const stringToArrayBuffer = (str: string): Uint8Array => {
    return new TextEncoder().encode(str);
};

// Helper function to convert ArrayBuffer to Base64
const arrayBufferToBase64 = (buffer: ArrayBuffer): string => {
    return btoa(String.fromCharCode(...new Uint8Array(buffer)));
};

// Helper function to convert Base64 to ArrayBuffer
const base64ToArrayBuffer = (base64: string): ArrayBuffer => {
    const binaryString = atob(base64);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes.buffer;
};

// Derive a crypto key from the passphrase
const getKey = async (): Promise<CryptoKey> => {
    const keyMaterial = await crypto.subtle.importKey(
        'raw',
        stringToArrayBuffer(ENCRYPTION_KEY),
        'PBKDF2',
        false,
        ['deriveKey']
    );

    return crypto.subtle.deriveKey(
        {
            name: 'PBKDF2',
            salt: stringToArrayBuffer('StaticSalt'),
            iterations: 100000,
            hash: 'SHA-256'
        },
        keyMaterial,
        { name: 'AES-GCM', length: 256 },
        false,
        ['encrypt', 'decrypt']
    );
};

export const encrypt = async (data: string): Promise<string> => {
    try {
        const key = await getKey();
        const iv = crypto.getRandomValues(new Uint8Array(12));
        const encodedData = stringToArrayBuffer(data);

        const encrypted = await crypto.subtle.encrypt(
            { name: 'AES-GCM', iv },
            key,
            encodedData
        );

        // Combine IV and encrypted data
        const buffer = new Uint8Array(iv.byteLength + encrypted.byteLength);
        buffer.set(iv, 0);
        buffer.set(new Uint8Array(encrypted), iv.byteLength);

        return arrayBufferToBase64(buffer);
    } catch (error) {
        console.error('Encryption failed:', error);
        throw new Error('Failed to encrypt data');
    }
};

export const decrypt = async (encryptedData: string): Promise<string> => {
    try {
        const key = await getKey();
        const buffer = base64ToArrayBuffer(encryptedData);

        // Extract IV and ciphertext
        const iv = buffer.slice(0, 12);
        const ciphertext = buffer.slice(12);

        const decrypted = await crypto.subtle.decrypt(
            { name: 'AES-GCM', iv: new Uint8Array(iv) },
            key,
            ciphertext
        );

        return new TextDecoder().decode(decrypted);
    } catch (error) {
        console.error('Decryption failed:', error);
        throw new Error('Failed to decrypt data');
    }
};