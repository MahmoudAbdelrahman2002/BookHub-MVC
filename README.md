# BookHub-MVC 📚

A comprehensive e-commerce web application for book management and sales, built with ASP.NET Core MVC and modern web technologies.

## 🌟 Overview

BookHub-MVC is a full-featured online bookstore that provides a complete e-commerce solution for book retailers. The application supports multiple user roles, shopping cart functionality, order management, and secure payment processing through Stripe integration.



## ✨ Features

### 🛍️ Customer Features
- **Browse Books**: View all available books with detailed information
- **Category Filtering**: Browse books by categories
- **Product Details**: Detailed book information including descriptions, pricing, and images
- **Shopping Cart**: Add/remove books from cart with quantity management
- **Order Management**: Place orders and track order history
- **User Authentication**: Secure registration and login with email confirmation
- **Social Login**: Facebook authentication integration
- **Profile Management**: Manage personal information and account settings

### 👨‍💼 Admin Features
- **Product Management**: Add, edit, and delete books from the catalog
- **Category Management**: Organize books into categories
- **User Management**: Manage customer accounts and roles
- **Company Management**: Manage business customer accounts
- **Order Processing**: View and manage all customer orders
- **Role-based Access Control**: Secure admin panel with different permission levels

### 💳 Payment & Security
- **Stripe Integration**: Secure payment processing
- **Multiple User Roles**: Admin, Employee, Customer, and Company users
- **Email Confirmation**: Account verification system
- **Secure Authentication**: ASP.NET Core Identity integration

## 🛠️ Technology Stack

### Backend
- **Framework**: ASP.NET Core 9.0 MVC
- **Database**: SQL Server with Entity Framework Core 9.0
- **Authentication**: ASP.NET Core Identity
- **Architecture**: N-tier architecture with Repository and Unit of Work patterns

### Frontend
- **UI Framework**: Bootstrap 5
- **Icons**: Bootstrap Icons
- **JavaScript**: jQuery
- **Styling**: CSS3 with custom styles

### External Services
- **Payment**: Stripe API
- **Social Auth**: Facebook Login
- **Email**: Email confirmation system

### Development Tools
- **ORM**: Entity Framework Core
- **Database Migrations**: Code-First approach
- **Dependency Injection**: Built-in ASP.NET Core DI container

## 📁 Project Structure

```
BookHub-MVC/
├── BulkyWeb/                    # Main web application
│   ├── Areas/
│   │   ├── Admin/              # Admin area controllers and views
│   │   ├── Customer/           # Customer area controllers and views
│   │   └── Identity/           # Identity pages
│   ├── Views/                  # Shared views and layouts
│   ├── wwwroot/               # Static files (CSS, JS, images)
│   └── Program.cs             # Application startup configuration
├── Bulky.DataAccess/          # Data access layer
│   ├── Data/                  # DbContext and database configuration
│   ├── Migrations/            # Entity Framework migrations
│   ├── Repository/            # Repository pattern implementation
│   └── DbInitializer/         # Database seeding and initialization
├── Bulky.Models/              # Domain models and DTOs
│   ├── Models/                # Entity models
│   └── ViewModels/            # View models for UI
├── Bulky.Utility/             # Utility classes and constants
└── Bulky.sln                  # Solution file
```

## 🚀 Getting Started

### Prerequisites
- **.NET 9.0 SDK** or later
- **SQL Server** (LocalDB or full SQL Server)
- **Visual Studio 2022** or **Visual Studio Code** (recommended)
- **Git** for version control

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/MahmoudAbdelrahman2002/BookHub-MVC.git
   cd BookHub-MVC
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Update database connection string**
   
   Edit `BulkyWeb/appsettings.json` and update the connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BookHubDB;Trusted_Connection=True;MultipleActiveResultSets=true"
     }
   }
   ```

4. **Configure Stripe (Optional)**
   
   For payment functionality, add your Stripe keys to `appsettings.json`:
   ```json
   {
     "stripe": {
       "PublishableKey": "your_publishable_key_here",
       "secretKey": "your_secret_key_here"
     }
   }
   ```

5. **Configure Facebook Authentication (Optional)**
   
   Update Facebook app credentials in `Program.cs` or use user secrets:
   ```csharp
   builder.Services.AddAuthentication().AddFacebook(options =>
   {
       options.AppId = "your_facebook_app_id";
       options.AppSecret = "your_facebook_app_secret";
   });
   ```

6. **Apply database migrations**
   ```bash
   dotnet ef database update --project BulkyWeb
   ```

7. **Run the application**
   ```bash
   dotnet run --project BulkyWeb
   ```

8. **Access the application**
   
   Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

### 🔑 Default Admin Account

The application will automatically create a default admin account on first run:
- **Email**: admin@bulky.com
- **Password**: Admin@123
- **Role**: Administrator

## 📝 Usage

### For Customers
1. **Browse Books**: Visit the home page to see all available books
2. **Register Account**: Create a new account or login with existing credentials
3. **Add to Cart**: Click on book details and add to your shopping cart
4. **Checkout**: Proceed to checkout and complete payment via Stripe
5. **Order History**: View your past orders in your account

### For Administrators
1. **Login**: Use admin credentials to access the admin panel
2. **Manage Products**: Add new books, edit existing ones, or remove discontinued items
3. **Manage Categories**: Organize books into logical categories
4. **User Management**: View and manage customer accounts
5. **Order Processing**: Monitor and process customer orders

## 🔧 Configuration

### Database Configuration
- The application uses Entity Framework Core with SQL Server
- Connection strings are configured in `appsettings.json`
- Database initialization and seeding happen automatically on startup

### Authentication Configuration
- ASP.NET Core Identity is configured for user management
- Password requirements and lockout policies can be adjusted in `Program.cs`
- Email confirmation is enabled by default

### Payment Configuration
- Stripe integration requires valid API keys
- Test keys are included for development purposes
- Production deployment requires live Stripe keys

## 🏗️ Architecture

The application follows a clean, layered architecture:

- **Presentation Layer** (`BulkyWeb`): MVC controllers, views, and web configuration
- **Business Logic Layer** (`Bulky.Models`): Domain models and business rules
- **Data Access Layer** (`Bulky.DataAccess`): Repository pattern, Unit of Work, and Entity Framework
- **Utility Layer** (`Bulky.Utility`): Shared utilities, constants, and helper classes

### Design Patterns Used
- **Repository Pattern**: For data access abstraction
- **Unit of Work Pattern**: For transaction management
- **Dependency Injection**: For loose coupling and testability
- **Model-View-Controller**: For separation of concerns

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License. See the `LICENSE` file for details.

## 👨‍💻 Developer

**Mahmoud Abdelrahman**
- GitHub: [@MahmoudAbdelrahman2002](https://github.com/MahmoudAbdelrahman2002)

## 🙏 Acknowledgments

- Built with ❤️ using ASP.NET Core
- UI components powered by Bootstrap
- Payment processing by Stripe
- Icons provided by Bootstrap Icons

## 📞 Support

If you encounter any issues or have questions, please:
1. Check the existing [issues](https://github.com/MahmoudAbdelrahman2002/BookHub-MVC/issues)
2. Create a new issue if your problem isn't already reported
3. Provide detailed information about the problem and your environment

---

**Happy coding! 🚀**