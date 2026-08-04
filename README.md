# Bulky - E-Commerce MVC Application

A full-featured e-commerce web application built with **ASP.NET Core MVC (.NET 8)** following the Udemy course "ASP.NET Core MVC (.NET 8)".

## 📋 Project Overview

Bulky is a comprehensive e-commerce platform designed to demonstrate modern web development practices using the ASP.NET Core MVC framework. The application includes complete functionality for managing products, categories, shopping carts, and orders.

## 🏗️ Architecture

The project follows a **layered architecture** approach with the following structure:

```
Bulky/
├── BulkyWeb/              # Main MVC web application
├── Bulky.Models/          # Data models and entities
├── Bulky.DataAccess/      # Data access layer (Database operations)
├── Bulky.Utility/         # Utility classes and helpers
└── Bulky.sln              # Solution file
```

### Project Layers

- **BulkyWeb**: Main ASP.NET Core MVC application containing controllers, views, and business logic
- **Bulky.Models**: Domain models and data entities
- **Bulky.DataAccess**: Entity Framework Core data access implementation and repositories
- **Bulky.Utility**: Helper classes, extensions, and utility functions

## ✨ Features

- ✅ Product management (CRUD operations)
- ✅ Category management
- ✅ Shopping cart functionality
- ✅ Order management
- ✅ User authentication and authorization
- ✅ Admin dashboard
- ✅ Responsive web interface

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core MVC (.NET 8)
- **Database**: SQL Server with Entity Framework Core
- **Frontend**: HTML5, CSS3, Bootstrap
- **Language**: C#

## 🚀 Getting Started

### Prerequisites

- .NET 8 SDK or later
- SQL Server (LocalDB or full version)
- Visual Studio 2022 or Visual Studio Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/AlperAricay/Bulky_MVC.git
   cd Bulky_MVC
   ```

2. **Open the solution**
   ```bash
   # Using Visual Studio
   Open Bulky.sln in Visual Studio
   
   # Or using CLI
   dotnet open Bulky.sln
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Update the database**
   ```bash
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run --project BulkyWeb
   ```

The application will be available at `https://localhost:7000` (or the port shown in your terminal).

## 📁 Project Structure

### BulkyWeb
Contains the main MVC components:
- **Controllers/**: Handle HTTP requests and business logic
- **Views/**: Razor templates for rendering HTML
- **Models/**: ViewModels for data binding
- **wwwroot/**: Static files (CSS, JavaScript, images)

### Bulky.Models
Domain models representing core entities:
- Product
- Category
- Order
- OrderDetail
- ShoppingCart
- User

### Bulky.DataAccess
Data access layer with:
- Entity Framework Core DbContext
- Repository pattern implementation
- Database migrations

### Bulky.Utility
Common utilities including:
- Constants
- Helper methods
- Extensions
- Configuration classes

## 💾 Database

The application uses SQL Server with Entity Framework Core for data access. Database migrations handle schema management automatically.

## 🔐 Security

- User authentication using ASP.NET Core Identity
- Authorization policies for admin operations
- Input validation and sanitization
- CSRF protection

## 📚 Learning Resource

This project is based on the **"ASP.NET Core MVC (.NET 8)"** course on Udemy, demonstrating:
- MVC architectural pattern
- Entity Framework Core usage
- Dependency Injection
- Repository pattern
- Authentication and authorization
- CRUD operations

## 📝 License

This project is open source and available for educational purposes.

## 👤 Author

Created by [AlperAricay](https://github.com/AlperAricay)

## 🤝 Contributing

Contributions are welcome! Feel free to fork this repository and submit pull requests.

---

**Note**: This project is primarily for educational purposes to learn ASP.NET Core MVC development.
