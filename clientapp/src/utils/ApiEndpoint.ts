const BASE_API_URL = 'https://localhost:7279/api';

export const apiEndpoint = {
    ApiURL: BASE_API_URL,

    AuthEndpoint: {
        login: '/Appointments/login',
        logout: '/Appointments/logout',
        refreshToken: '/Appointments/refreshToken',
    },

    AppointmentEndpoint: {
        list: '/Appointments/list'
    }
} as const;