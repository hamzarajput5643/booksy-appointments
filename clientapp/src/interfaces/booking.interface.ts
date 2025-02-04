export interface BookingData {
    bookings: { [key: number]: Booking };
}

export interface Booking {
    id: number;
    _version: number;
    partner_app_data: Record<string, any>;
    type: string;
    status: string;
    multibooking: Multibooking;
    appointment_uid: number;
    repeating: any | null;
    booked_from: string;
    booked_till: string;
    updated_epoch: number;
    resources: Resource[];
    resource_ids: any | null;
    combo_parent_id: any | null;
    service: Service;
    actions: Actions;
    customer: Customer;
    _editable: boolean;
    customer_note: string | null;
    has_addons: boolean;
    has_note: boolean;
    paid: boolean;
    is_highlighted: boolean;
    is_staffer_requested_by_client: boolean;
    autoassign: boolean;
    payable: boolean;
    from_promo: boolean;
    review: any | null;
}

export interface Multibooking {
    id: number;
    _version: number;
}

export interface Resource {
    id: number;
}

export interface Service {
    id: number;
    name: string;
    color: number;
    service_category_id: number;
    variant_label: string;
}

export interface Actions {
    cancel: boolean;
    cancel_no_show: boolean;
    change: boolean;
    change_time_or_note: boolean;
    confirm: boolean;
    decline: boolean;
    no_show: boolean;
}

export interface Customer {
    id: number;
    name: string;
    photo: string;
    phone: string;
}