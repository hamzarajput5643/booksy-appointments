import { useEffect } from "react";
import axios from "axios";
import useAuthStore from "../store/store";
import { authService } from "../services/apiServices";

export const AxiosApiInterceptor = () => {
    const { accessToken, refreshToken: refresh, setTokens, resetTokens } = useAuthStore();

    useEffect(() => {
        const requestInterceptor = axios.interceptors.request.use((config) => {
            if (accessToken && !config.headers.Authorization) {
                config.headers.Authorization = `Bearer ${accessToken}`;
            }
            return config;
        });

        const responseInterceptor = axios.interceptors.response.use(
            (response) => response,
            async (error) => {
                if (error.response?.status === 401 && refresh && accessToken) {
                    try {
                        const response = await authService.refreshToken({ accessToken, refreshToken: refresh });
                        if (response?.isValid && response.data) {
                            setTokens(response.data);
                            error.config.headers.Authorization = `Bearer ${response.data.accessToken}`;
                            return axios.request(error.config);
                        } else {
                            resetTokens();
                        }
                    } catch {
                        resetTokens();
                    }
                }
                return Promise.reject(error);
            }
        );

        return () => {
            axios.interceptors.request.eject(requestInterceptor);
            axios.interceptors.response.eject(responseInterceptor);
        };
    }, [accessToken, refresh, setTokens, resetTokens]);

    return null;
};