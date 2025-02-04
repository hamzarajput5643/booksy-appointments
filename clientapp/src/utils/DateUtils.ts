/**
 * Date formatting utilities for API communication
 */
export class DateUtils {
    /**
     * Formats Date objects to API-safe ISO date strings (YYYY-MM-DD)
     * @param date - Date object to format
     * @returns ISO date string without time component
     */
    static formatISODate(date: Date): string {
        return date.toISOString().split('T')[0];
    }

    /**
     * Validates date ranges for API queries
     * @param startDate - Start of date range
     * @param endDate - End of date range
     * @throws Error if dates are invalid
     */
    static validateDateRange(startDate: Date, endDate: Date): void {
        if (startDate > endDate) {
            throw new Error('Invalid date range: startDate must be before endDate');
        }

        if (isNaN(startDate.getTime()) || isNaN(endDate.getTime())) {
            throw new Error('Invalid date parameters');
        }
    }
}