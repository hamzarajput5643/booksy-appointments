import { ApiClient } from '../utils/ApiRequest';
import { RequestResponse } from '../common/RequestResponse';
import { LoginResponse } from '../common/LoginResponse';
import { apiEndpoint } from '../utils/ApiEndpoint';
import { DateUtils } from '../utils/DateUtils';
import { AppointmentQueryParams } from '../models/AppointmentQueryParams';
import { TokenRefreshParams } from '../models/TokenRefreshParams';
import { data } from 'react-router-dom';

// Configuration interface for API requests
interface ApiConfig {
    timeout?: number;
    retries?: number;
    headers?: Record<string, string>;
}

// Default API configuration
const DEFAULT_API_CONFIG: ApiConfig = {
    timeout: 5000,
    retries: 1,
    headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
    }
};

export const authService = {
    /**
     * Refreshes authentication tokens
     * @param credentials Token refresh credentials
     * @returns Promise with new authentication tokens
     */
    async refreshToken(credentials: TokenRefreshParams): Promise<RequestResponse<LoginResponse>> {
        if (!credentials.accessToken || !credentials.refreshToken) {
            throw new Error('Invalid token refresh credentials');
        }

        return await ApiClient.post<RequestResponse<LoginResponse>>(
            apiEndpoint.AuthEndpoint.refreshToken,
            credentials,
            DEFAULT_API_CONFIG
        );
    },

    /**
     * Handles user login
     * @returns Promise with login response
     */
    async login(): Promise<RequestResponse<LoginResponse>> {
        return await ApiClient.get<RequestResponse<LoginResponse>>(
            apiEndpoint.AuthEndpoint.login,
            DEFAULT_API_CONFIG
        );
    },

    /**
     * Handles user logout
     * @returns Promise indicating logout success
     */
    async logout(): Promise<RequestResponse<boolean>> {
        return await ApiClient.get<RequestResponse<boolean>>(
            apiEndpoint.AuthEndpoint.logout,
            DEFAULT_API_CONFIG
        );
    }
};

export const appointmentService = {
    /**
     * Fetches appointments within a date range
     * @param params Query parameters including date range and optional filters
     * @returns Promise with filtered appointments
     */
    async getAppointments(params: AppointmentQueryParams): Promise<RequestResponse<object>> {
        // Use DateUtils for validation and formatting
        DateUtils.validateDateRange(params.startDate, params.endDate);

        const queryParams = {
            startDate: DateUtils.formatISODate(params.startDate),
            endDate: DateUtils.formatISODate(params.endDate),
            customerName: params.customerName?.trim()
        };

        return await ApiClient.post<RequestResponse<object>>(
            apiEndpoint.AppointmentEndpoint.list,
            queryParams,
            {
                ...DEFAULT_API_CONFIG
            }
        );
    }
};