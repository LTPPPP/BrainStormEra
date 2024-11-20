# BrainStormEra Project

## Overview

BrainStormEra is a comprehensive Course and Certificate management system designed to streamline the process of managing educational courses and certificates. Built using ASP.NET, the project follows a 3-layer architecture and adheres to the MVC (Model-View-Controller) pattern. The backend is developed using ASP.NET MVC, with the Entity Framework Database First approach for efficient database interactions. The system includes robust session management and utilizes SQL Server for database operations.

## Role :

1. **Admin**
2. **Instructor**
3. **Learner**

## Features:

1. **User Registration and Authentication:**

   - Register: Allows new users to create an account.
   - Login: Allows users to log in to their accounts.
   - Forgot Password: Allows users to reset their password if forgotten.
   - Logout: Allows users to securely log out of their accounts.

2. **Course Management:**

   - Create Course: Allows instructors to create new courses.
   - View Course Detail: Allows users to view detailed information about a course.
   - Enroll Course: Allows learners to enroll in a course.
   - Study Lesson: Allows learners to access and study lessons within a course.
   - Mark Lesson Complete: Allows learners to mark a lesson as completed.
   - Update Course: Allows instructors to update course information.
   - Delete Course: Allows instructors to delete a course.
   - Create Chapter: Allows instructors to add chapters to a course.
   - View Chapter: Allows users to view chapter details.
   - Update Chapter: Allows instructors to update chapter information.
   - Delete Chapter: Allows instructors to delete a chapter.
   - Create Lesson: Allows instructors to add lessons to a chapter.
   - View Lesson: Allows users to view lesson details.
   - Update Lesson: Allows instructors to update lesson information.
   - Delete Lesson: Allows instructors to delete a lesson.

3. **Notification Management:**

   - Create Notification: Allows administrators to create notifications.
   - View Notification: Allows users to view notifications.
   - Update Notification: Allows administrators to update notifications.
   - Delete Notification: Allows administrators to delete notifications.

4. **Certificate Management:**

   - View Certificate: Allows users to view their course completion certificates.
   - View Detail Certificate: Allows users to view detailed information of their certificates.

5. **Feedback Management:**

   - Create Feedback: Allows learners to submit feedback on courses.
   - View Feedback: Allows users to view feedback submitted by learners.
   - Update Feedback: Allows learners to update their feedback.
   - Delete Feedback: Allows learners or administrators to delete feedback.

6. **Achievement Management:**

   - View Achievements: Allows learners to view their achievements.
   - Create Achievement: Allows administrators to create new achievements.
   - Update Achievement: Allows administrators to update achievement information.
   - Delete Achievement: Allows administrators to delete achievements.

7. **Profile Management:**

   - Update Profile: Allows users to update their profile information.
   - View Profile: Allows users to view their profile information.
   - Reset Password: Allows users to reset their password.

8. **Chatbot Interaction:**

   - Interact Chatbot: Allows users to interact with a chatbot for support and guidance.
   - View Chatbot History: Allows administrators to view chatbot interaction history.

9. **User Management:**

   - View User: Allows administrators to view user profiles.
   - Ban User: Allows administrators to ban a user.
   - Promote to Instructor: Allows administrators to promote a learner to an instructor role.

10. **Course Acceptance:**

    - Change Status: Allows administrators to approve or reject courses submitted by instructors.

11. **Payment Management:**

    - View Payment: Allows administrators to view payment transactions.
    - Update Payment: Allows administrators to update user points.

12. **View User Ranking:**

    - Ranking: Allows users to view their ranking based on course completion and achievements.

13. **Create Payment Request:**

    - Create Payment Request: Allows learners to create a payment request by uploading transaction confirmation.

14. **View Learner Certificate:**

    - View Learner Certificate: Allows administrators to view certificates of learners who have completed courses.

15. **View Reporting Data:**
    - View Reporting Data: Allows administrators to view data reports on chatbot usage, user activities, and course creation statistics.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version 8.0 or higher)
- [SQL Server 2019](https://www.microsoft.com/en-us/sql-server/sql-server-2019) SQL Server 2019 or later

## Installation

To set up the project and install necessary dependencies, follow these steps:

1. **Clone the Repository:**

   ```bash
   git clone https://github.com/LTPPPP/BrainStormEra
   ```

   ```bash
   cd BrainStormEra
   ```

2. **Install .NET SDK:**
   Ensure you have the .NET SDK installed. You can download it from [here](https://dotnet.microsoft.com/download).

3. **Install SQL Server:**
   Make sure SQL Server 2019 or later is installed and running. You can download it from [here](https://www.microsoft.com/en-us/sql-server/sql-server-2019).

4. **Set Up the Database:**
   Update the connection string in `appsettings.json` to match your SQL Server configuration.

   ```json
   "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
   },
   "AllowedHosts": "*",
   "ConnectionStrings": {
   "DefaultConnection": "Server=your_server_name;Database=BrainStormEra;User Id=your_username;Password=your_password;"
    },
   "GeminiApiKey": "",
   "GeminiApiUrl": "",
   "SmtpSettings": {
    "Server": "",
    "Port": "",
    "EnableSsl": true,
    "SenderEmail": "",
    "SenderName": "",
    "Username": "",
    "Password": ""
   }
   ```

5. **Install Dependencies:**
   Run the following commands to install necessary packages:

   ```bash
   dotnet add package System.Net.Http
   dotnet add package Microsoft.EntityFrameworkCore
   dotnet add package Microsoft.EntityFrameworkCore.Tools
   dotnet add package Microsoft.EntityFrameworkCore.Design
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   dotnet add package Microsoft.AspNetCore.Session
   dotnet add package Newtonsoft.Json
   dotnet add package Markdig
   dotnet add package System.Data.SqlClient
   dotnet add package Microsoft.CognitiveServices.Speech
   dotnet add package Google.Apis.YouTube.v3
   dotnet add package itext7
   dotnet add package PdfSharp
   dotnet add package itextsharp
   ```

6. **Run Migrations:**
   Apply the database migrations to set up the database schema.

   ```bash
   dotnet ef database update
   ```

7. **Run the Application:**
   Start the application using the following command:

   ```bash
   dotnet run build
   ```

8. **Access the Application:**
   Open your web browser and navigate to `http://localhost:5289` or `"https://localhost:7252` to access the BrainStormEra application.

By following these steps, you should have the BrainStormEra project up and running on your local machine.
