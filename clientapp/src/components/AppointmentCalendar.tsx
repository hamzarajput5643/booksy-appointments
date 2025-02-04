import React, { useState, useEffect } from 'react';
import moment from 'moment';
import { Calendar, momentLocalizer, Views } from 'react-big-calendar';
import 'react-big-calendar/lib/css/react-big-calendar.css';
import { appointmentService } from '../services/apiServices';
import './Calendar.css';

const localizer = momentLocalizer(moment);

interface Customer {
    id: number;
    name: string;
    photo: string | null;
    phone: string;
}

interface Booking {
    actions: { [key: string]: boolean };
    appointment_uid: number;
    autoassign: boolean;
    booked_from: string;
    booked_till: string;
    combo_parent_id: null | number;
    customer: Customer;
    customer_note: null | string;
}

interface Appointments {
    bookings?: {
        [key: number]: Booking;
    };
}

const AppointmentCalendar: React.FC = () => {
    const [appointments, setAppointments] = useState<Appointments>({});
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [date, setDate] = useState(new Date());
    const [filters, setFilters] = useState({
        startDate: moment().startOf('day').toDate(),
        endDate: moment().endOf('day').toDate(),
        customerName: '',
    });

    useEffect(() => {
        const fetchAppointments = async () => {
            try {
                setLoading(true);
                setError('');
                const data = await appointmentService.getAppointments({
                    startDate: filters.startDate,
                    endDate: filters.endDate,
                    customerName: filters.customerName
                });
                setAppointments(data.data);
            } catch (err) {
                setError('Failed to fetch appointments');
            } finally {
                setLoading(false);
            }
        };

        const debounceTimer = setTimeout(fetchAppointments, 500);
        return () => clearTimeout(debounceTimer);
    }, [filters]);

    const events = Object.values(appointments.bookings || {}).map(booking => ({
        id: booking.appointment_uid,
        title: booking.customer.name,
        start: new Date(booking.booked_from),
        end: new Date(booking.booked_till),
        customer: booking.customer,
        phone: booking.customer.phone,
    }));

    const handleNavigate = (newDate: Date, view: string, action: string) => {
        setDate(newDate);
        setFilters(prev => ({
            ...prev,
            startDate: moment(newDate).startOf('day').toDate(),
            endDate: moment(newDate).endOf('day').toDate(),
        }));
    };

    const EventComponent = ({ event }: any) => (
        <div className="event-container">
            <div className="font-medium">{event.title}</div>
            <div className="text-sm">
                {moment(event.start).format('h:mm')} - {moment(event.end).format('h:mm A')}
            </div>
            <div className="text-sm text-gray-600">{event.phone || 'No phone'}</div>
        </div>
    );

    return (
        <div className="appointment-calendar p-4 h-screen">
            <div className="mb-4 flex items-center justify-between">
                <div className="flex gap-2">
                    <input
                        type="text"
                        placeholder="Filter by customer name"
                        className="px-4 py-2 border rounded"
                        value={filters.customerName}
                        onChange={(e) => setFilters(prev => ({
                            ...prev,
                            customerName: e.target.value
                        }))}
                    />
                </div>
                <div className="font-bold">
                    {moment(filters.startDate).format('MMM D')} -{' '}
                    {moment(filters.endDate).format('MMM D, YYYY')}
                </div>
            </div>

            {loading && <div className="p-4 text-center">Loading appointments...</div>}
            {error && <div className="p-4 text-red-500">{error}</div>}

            <Calendar
                localizer={localizer}
                events={events}
                defaultView={Views.WEEK}
                view={Views.WEEK}
                date={date}
                onNavigate={handleNavigate}
                min={new Date(0, 0, 0, 8, 0, 0)} // 8:00 AM
                max={new Date(0, 0, 0, 20, 0, 0)} // 8:00 PM
                components={{
                    event: EventComponent
                }}
                formats={{
                    timeGutterFormat: 'h:mm A',
                    eventTimeRangeFormat: () => '' // Hide time in event as we show it in custom component
                }}
                step={15}
                timeslots={4}
            />
        </div>
    );
};

export default AppointmentCalendar;