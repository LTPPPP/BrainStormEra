# BrainStormEra Project

## Table of Contents

1. [Overview](#overview)
2. [Technologies Used](#technologies-used)
3. [Role](#role)
4. [Features](#features)
5. [Prerequisites](#prerequisites)
6. [Installation](#installation)
7. [GEMINI_KEY](#getting-gemini-api-key)
8. [Contributing](#contributing)
9. [License](#license)
10. [Contact](#contact)

## Overview

BrainStormEra is a comprehensive Course and Certificate management system designed to streamline the process of managing educational courses and certificates. Built using ASP.NET, the project follows a 3-layer architecture and adheres to the MVC (Model-View-Controller) pattern. The backend is developed using ASP.NET MVC, The system includes robust session management and utilizes SQL Server 2019 for database operations.

## Technologies Used

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![HTML](https://img.shields.io/badge/HTML-239120?style=for-the-badge&logo=html5&logoColor=orange)
![CSS](https://img.shields.io/badge/CSS-239120?style=for-the-badge&logo=css3&logoColor=blue)
![JavaScript](https://img.shields.io/badge/JavaScript-323330?style=for-the-badge&logo=javascript&logoColor=F7DF1E)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL](https://img.shields.io/badge/SQL-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-563D7C?style=for-the-badge&logo=bootstrap&logoColor=white)
![Entity Framework](https://img.shields.io/badge/Entity%20Framework-512BD4?style=for-the-badge&logo=.net&logoColor=white)
![ASP.NET MVC](https://img.shields.io/badge/ASP.NET%20MVC-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Newtonsoft.Json](https://img.shields.io/badge/Newtonsoft.Json-000000?style=for-the-badge&logo=json&logoColor=white)
![Markdig](https://img.shields.io/badge/Markdig-000000?style=for-the-badge&logo=markdown&logoColor=white)
![System.Data.SqlClient](https://img.shields.io/badge/System.Data.SqlClient-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Microsoft Cognitive Services](https://img.shields.io/badge/Microsoft%20Cognitive%20Services-0078D7?style=for-the-badge&logo=microsoft&logoColor=white)
![Google APIs](https://img.shields.io/badge/Google%20APIs-4285F4?style=for-the-badge&logo=google&logoColor=white)

## Role

1. **Admin**
2. **Instructor**
3. **Learner**

## Features

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
- [Visual Studio](https://visualstudio.microsoft.com/) (recommended for development)
- [Git](https://git-scm.com/) (for version control)

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

## Getting GEMINI API Key

To use the GEMINI API in the BrainStormEra project, follow these steps to obtain your API key:

1. **Sign Up for GEMINI:**
   Visit the [GEMINI website](https://ai.google.dev/gemini-api/docs) and sign up for an account if you don't already have one.

2. **Navigate to API Settings:**
   Once logged in, go to your account settings and find the API section.

3. **Create a New API Key:**
   Create a new API key by following the instructions provided. Make sure to set the appropriate permissions for the key.

4. **Copy the API Key:**
   Copy the generated API key and keep it secure. You will need this key to configure the BrainStormEra project.

5. **Update `appsettings.json`:**
   Add your GEMINI API key to the `appsettings.json` file in the project.

   ```json
   "GeminiApiKey": "your_gemini_api_key",
   "GeminiApiUrl": "https://api.gemini.com/v1"
   ```

By following these steps, you should have the BrainStormEra project up and running on your local machine with the GEMINI API configured.

## Contributing

We welcome contributions to the BrainStormEra project. If you would like to contribute, please follow these steps:

1. **Fork the Repository:**
   Click the "Fork" button at the top right of the repository page to create a copy of the repository in your GitHub account.

2. **Clone the Forked Repository:**
   Clone the forked repository to your local machine.

   ```bash
   git clone https://github.com/LTPPPP/BrainStormEra
   ```

3. **Create a New Branch:**
   Create a new branch for your feature or bug fix.

   ```bash
   git checkout -b feature/your-feature-name
   ```

4. **Make Your Changes:**
   Make your changes to the codebase.

5. **Commit Your Changes:**
   Commit your changes with a descriptive commit message.

   ```bash
   git commit -m "Add feature: your feature name"
   ```

6. **Push Your Changes:**
   Push your changes to your forked repository.

   ```bash
   git push origin feature/your-feature-name
   ```

7. **Create a Pull Request:**
   Open a pull request to the main repository with a description of your changes.

## License

This project is licensed under the MIT License. See the [LICENSE](https://drive.google.com/drive/folders/1SWbiUoTGMQEZghKBikA88Ud9a6Zo3y1N) file for more information.

## Contact

For any questions or inquiries, please contact us at [brainstormera.pro@gmail.com](mailto:brainstormera.pro@gmail.com).
