const TRUSTED_ISSUER = import.meta.env.VITE_JWT_ISSUER;

export const validateTokenStructure = (token: string): boolean => {
    return token.split(".").length === 3;
};

export const validateTokenIssuer = (issuer?: string): boolean => {
    if (!issuer) return false;
    if (!TRUSTED_ISSUER) throw new Error('Missing JWT issuer configuration');
    return issuer === TRUSTED_ISSUER;
};

export const validateTokenExpiration = (exp?: number): boolean => {
    if (!exp) return false;
    return exp * 1000 > Date.now();
};