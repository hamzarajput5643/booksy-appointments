import { toast } from "react-toastify";

/**
 * Handles API errors and displays appropriate error messages using toast notifications.
 * @param error - The error object caught during the API request.
 */
export function handleApiError(error: any): void {
    if (error.response) {
        // The server responded with a status code outside the 2xx range
        const status = error.response.status;
        const message = error.response.data?.message || error.response.statusText;

        // Handle common HTTP status codes
        switch (status) {
            case 400:
                toast.error(`Bad Request: ${message}`);
                break;
            case 401:
                toast.error(`Unauthorized: ${message}`);
                break;
            case 403:
                toast.error(`Forbidden: ${message}`);
                break;
            case 404:
                toast.error(`Not Found: ${message}`);
                break;
            case 500:
                toast.error(`Server Error: ${message}`);
                break;
            default:
                toast.error(`Error ${status}: ${message}`);
                break;
        }
    } else if (error.request) {
        // The request was made, but no response was received
        toast.error("No response received from server. Please check your internet connection.");
    } else {
        // Something happened in setting up the request
        toast.error(`Error: ${error.message}`);
    }
}

/**
 * Custom Error Handling for Website-Specific Exceptions (Client-Side Errors).
 * @param error - The error object caught on the website (e.g., form validation, UI issues).
 */
export function handleWebsiteError(error: Error): void {
    // For client-side issues, log the error for debugging purposes
    console.error("Website Error: ", error);

    // Show a user-friendly message
    toast.error("An unexpected issue occurred on the website. Please try again later.");
}