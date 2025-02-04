import axios, { AxiosRequestConfig, Method, CancelTokenSource } from 'axios';
import { handleApiError } from './Exceptions';
import { apiEndpoint } from './ApiEndpoint';

interface RequestOptions<TData> extends Omit<AxiosRequestConfig, 'data' | 'method' | 'url'> {
    retries?: number;
    retryDelay?: number;
    timeout?: number;
    validateStatus?: (status: number) => boolean;
    transformResponse?: (data: any) => TData;
}

class ApiError extends Error {
    constructor(
        public status: number,
        public code: string,
        message: string,
        public data?: any
    ) {
        super(message);
        this.name = 'ApiError';
    }
}

const defaultOptions: Partial<RequestOptions<any>> = {
    retries: 3,
    retryDelay: 1000,
    timeout: 30000,
    validateStatus: (status: number) => status >= 200 && status < 300,
};

class ApiClient {
    private static baseURL: string = '';
    private static cancelTokens: Map<string, CancelTokenSource> = new Map();
    private static defaultHeaders: Record<string, string> = {};

    static setBaseURL(url: string): void {
        this.baseURL = url.endsWith('/') ? url.slice(0, -1) : url;
    }

    static setDefaultHeaders(headers: Record<string, string>): void {
        this.defaultHeaders = headers;
    }

    private static getFullURL(endpoint: string): string {
        const path = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
        return `${this.baseURL}${path}`;
    }

    private static async delay(ms: number): Promise<void> {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    static cancelRequest(requestId: string): void {
        const source = this.cancelTokens.get(requestId);
        if (source) {
            source.cancel('Request cancelled by user');
            this.cancelTokens.delete(requestId);
        }
    }

    static async request<TResponse, TData = unknown>(
        url: string,
        data?: TData,
        method: Method = 'post',
        options: RequestOptions<TResponse> = {}
    ): Promise<TResponse> {
        const fullUrl = this.getFullURL(url);
        const mergedOptions = { ...defaultOptions, ...options };
        const { retries, retryDelay, ...axiosOptions } = mergedOptions;

        const source = axios.CancelToken.source();
        const requestId = `${method}-${fullUrl}-${Date.now()}`;
        this.cancelTokens.set(requestId, source);

        let lastError: Error | null = null;

        for (let attempt = 0; attempt <= retries!; attempt++) {
            try {
                const response = await axios({
                    url: fullUrl,
                    method,
                    data,
                    headers: {
                        ...this.defaultHeaders,
                        ...axiosOptions.headers
                    },
                    cancelToken: source.token,
                    ...axiosOptions,
                });

                this.cancelTokens.delete(requestId);

                if (mergedOptions.transformResponse) {
                    return mergedOptions.transformResponse(response.data);
                }
                return response.data;

            } catch (error: any) {
                lastError = error;

                if (axios.isCancel(error)) {
                    throw new ApiError(499, 'REQUEST_CANCELLED', 'Request was cancelled');
                }

                if (error.response) {
                    const status = error.response.status;

                    // Don't retry client errors except for 429 (Too Many Requests)
                    if (status >= 400 && status < 500 && status !== 429) {
                        throw new ApiError(
                            status,
                            error.response.data?.code || 'CLIENT_ERROR',
                            error.response.data?.message || 'Client error occurred',
                            error.response.data
                        );
                    }
                }

                if (attempt < retries!) {
                    await this.delay(retryDelay! * Math.pow(2, attempt));
                    continue;
                }
            }
        }

        handleApiError(lastError!);
        throw new ApiError(500, 'REQUEST_FAILED', 'Request failed after retries');
    }

    static async get<TResponse>(
        url: string,
        options?: RequestOptions<TResponse>
    ): Promise<TResponse> {
        return this.request<TResponse>(url, undefined, 'get', options);
    }

    static async post<TResponse, TData = unknown>(
        url: string,
        data: TData,
        options?: RequestOptions<TResponse>
    ): Promise<TResponse> {
        return this.request<TResponse, TData>(url, data, 'post', options);
    }

    static async put<TResponse, TData = unknown>(
        url: string,
        data: TData,
        options?: RequestOptions<TResponse>
    ): Promise<TResponse> {
        return this.request<TResponse, TData>(url, data, 'put', options);
    }

    static async delete<TResponse>(
        url: string,
        options?: RequestOptions<TResponse>
    ): Promise<TResponse> {
        return this.request<TResponse>(url, undefined, 'delete', options);
    }
}

ApiClient.setBaseURL(apiEndpoint.ApiURL);

export { ApiClient, ApiError, type RequestOptions };