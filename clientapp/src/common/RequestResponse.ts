export interface RequestResponse<T> {
    message: string;
    data: T;
    isValid: boolean;
    errors: any;
    statusCode: number;
}