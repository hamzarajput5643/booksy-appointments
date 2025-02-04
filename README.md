# Booksy Authentication & Calendar Data Retrieval

## Project Overview

This project demonstrates how to authenticate a user via Booksy, retrieve appointment data using the Booksy API, and display the data in a filterable, calendar-based UI. The back-end is built with .NET Core, and the front-end is developed using React (with TypeScript).

---

## Prerequisites

Before setting up the project, ensure you have the following tools installed:

- **[Visual Studio](https://visualstudio.microsoft.com/)** or **[Visual Studio Code](https://code.visualstudio.com/)** for the .NET Core and React project.
- **[Node.js](https://nodejs.org/)** – required for managing JavaScript dependencies (React app).
- **.NET Core SDK** – needed to build and run the .NET Core backend.

---

## Setup & Installation

### Back-end Setup (.NET Core)

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/hamzarajput5643/booksy-appointments.git

## How Authentication is Handled

Authentication is handled by utilizing Booksy's Business API to retrieve business details. The process follows these steps:

1. **Business Authentication**: 
   We use Booksy's Business API to authenticate the user and retrieve business-related data. The **Business ID** is extracted from the API response and stored in the user claims. 

2. **JWT Token Generation**:
   Once authenticated, a JWT token is generated containing the business information and other necessary details. This token is then sent to the user’s client (React app) as part of the authentication process.

3. **Token Management**:
   On the user side, the **React app** uses **Zustand** (a state management library) to securely manage and store the JWT token. The token is kept in the browser's local storage or secure session storage to prevent exposure of sensitive data.

4. **API Access**:
   Whenever the user requests appointment data or any other business-related information, the stored token is sent in the **Authorization header** to ensure secure API access.

5. **Appointment Data Retrieval**:
   Using the valid JWT token, all appointments related to the business are retrieved and displayed in the calendar view, ensuring that only authenticated users can view their respective appointments.

This approach ensures that the authentication process is secure, the JWT token is stored safely on the client-side, and users can only access their own business and appointment details.

## API Calls Used and Response Structure

### API Endpoints

1. **Authentication Endpoints**:
   - **Login**: 
     - Endpoint: `/Appointments/login`
     - Purpose: Authenticates the user and returns a JWT token.
   - **Logout**: 
     - Endpoint: `/Appointments/logout`
     - Purpose: Logs the user out and invalidates the session.
   - **Refresh Token**: 
     - Endpoint: `/Appointments/refreshToken`
     - Purpose: Refreshes the JWT token for the user session.

2. **Appointment Endpoints**:
   - **List Appointments**: 
     - Endpoint: `/Appointments/list`
     - Purpose: Retrieves all appointments associated with the authenticated business.

### Response Structure

All API responses follow the following structure:

```csharp
public class RequestResponse
{
    public string Message { get; set; } = "success";
    public object Data { get; set; }
    public bool IsValid { get; set; } = true;
    public object Errors { get; set; }
    public int StatusCode { get; set; } = 200;
}