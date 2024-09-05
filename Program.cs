using System.Collections.Generic;
using System.Data.SqlClient;
using System;


class Program
{
    // Connection string to the database
    static string connectionString = @"Data Source=DESKTOP-6RT5AA5;Initial Catalog=SecureVault;Integrated Security=True;Encrypt=False";
    static string title;
    // Main entry point of the application
    static void Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan; // Set console text color
        DisplayTitle(); // Display the application title

        Login(); // Start the login process
    }

    // Displays the application title in the center of the console window
    static void DisplayTitle()
    {
        Console.Clear(); // Clear the console
       title = @".·:''''''''''''''''''''''''''''''''''''''''''''''''''''''''':·.
: :  ____                         __     __          _ _    : :
: : / ___|  ___  ___ _   _ _ __ __\ \   / /_ _ _   _| | |_  : :
: : \___ \ / _ \/ __| | | | '__/ _ \ \ / / _` | | | | | __| : :
: :  ___) |  __/ (__| |_| | | |  __/\ V / (_| | |_| | | |_  : :
: : |____/ \___|\___|\__,_|_|  \___| \_/ \__,_|\__,_|_|\__| : :
'·:.........................................................:·'";

        int consoleWidth = Console.WindowWidth; // Get console width
        string[] lines = title.Split(new[] { Environment.NewLine }, StringSplitOptions.None); // Split title into lines

        // Center each line and print it
        foreach (string line in lines)
        {
            int padding = (consoleWidth - line.Length) / 2; // Calculate padding for centering
            Console.WriteLine(new string(' ', padding) + line); // Print centered line
        }
    }

    // Handles user login process
    static void Login()
    {
        bool isLoggedIn = false; // Flag to indicate if login is successful

        while (!isLoggedIn)
        {
            try
            {
                Console.Write("\nEnter your User ID: ");
                if (!int.TryParse(Console.ReadLine(), out int userId) || userId <= 0)
                {
                    Console.WriteLine("Invalid User ID. It must be a positive integer.");
                    continue; // Prompt for login again
                }

                Console.Write("Enter your PIN: ");
                string enteredPIN = Console.ReadLine(); // Read user PIN
                Console.Clear(); // Clear the console
                Console.WriteLine(title);

                // Check if PIN is not empty
                if (string.IsNullOrWhiteSpace(enteredPIN))
                {
                    Console.WriteLine("PIN cannot be empty or whitespace.");
                    continue; // Prompt for login again
                }

                // Connect to database and validate user credentials
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open(); // Open database connection
                    SqlCommand command = new SqlCommand("SELECT UserID, UserName FROM Users WHERE UserID = @UserID AND EncryptedPIN = @PIN", connection);
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@PIN", enteredPIN);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read()) // Check if user exists
                        {
                            string userName = reader.GetString(1);
                            Console.WriteLine($"\nWelcome, {userName}!\n");
                            isLoggedIn = true; // Set flag to true to exit loop
                            ShowMenu(userId); // Show main menu
                        }
                        else
                        {
                            Console.WriteLine("Invalid User ID or PIN. Please try again.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during login: {ex.Message}");
            }
        }
    }


    // Displays the main menu and handles user choices
    static void ShowMenu(int userId)
    {
        bool exit = false;
        while (!exit)
        {
            // Display menu options
            Console.WriteLine("1. Deposit Money");
            Console.WriteLine("2. Withdraw Money");
            Console.WriteLine("3. View Balance");
            Console.WriteLine("4. View Past Transactions");
            Console.WriteLine("5. Exit");

            string choice = Console.ReadLine(); // Read user choice
            Console.Clear(); // Clear the console
            Console.WriteLine(title);

            try
            {
                // Handle user choice
                switch (choice)
                {
                    case "1":
                        DepositMoney(userId); // Call method to deposit money
                        break;
                    case "2":
                        WithdrawMoney(userId); // Call method to withdraw money
                        break;
                    case "3":
                        ViewBalance(userId); // Call method to view balance
                        break;
                    case "4":
                        ViewPastTransactions(userId); // Call method to view past transactions
                        break;
                    case "5":
                        Login();
                        //exit = true; // Exit the menu loop
                        break;
                    default:
                        Console.WriteLine("\nInvalid choice. Please select a valid option.");
                        break;
                }
            }
            catch
            {
                Console.WriteLine("Please enter a valid input");
                Console.Clear();
                ShowMenu(userId);
            }
        }
    }

    // Allows the user to select an account and retrieves its balance
    static int SelectAccount(int userId, out decimal balance)
    {
        balance = 0;
        List<int> accountIds = new List<int>();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open(); // Open database connection
            string query = "SELECT AccountID, AccountType, AccountNumber FROM Accounts WHERE UserID = @UserID";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserID", userId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        Console.WriteLine("\nNo accounts found.");
                        return -1;
                    }

                    // Display available accounts
                    Console.WriteLine("\nAvailable Accounts:");
                    while (reader.Read())
                    {
                        int accountId = reader.GetInt32(0);
                        string accountType = reader.GetString(1);
                        string accountNumber = reader.GetString(2);
                        accountIds.Add(accountId);
                        Console.WriteLine($"AccountID: {accountId}, AccountType: {accountType}, AccountNumber: {accountNumber}");
                    }
                }
            }

            // Prompt user to select an account
            while (true)
            {
                Console.Write("\nEnter AccountID: ");
           
                if (!int.TryParse(Console.ReadLine(), out int selectedAccountId) || !accountIds.Contains(selectedAccountId))
                {
                    Console.WriteLine("\nInvalid AccountID. Please enter a valid AccountID from the list.");
                    continue;
                }

                // Retrieve the balance of the selected account
                query = "SELECT AccountID, Balance FROM Accounts WHERE AccountID = @AccountID";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@AccountID", selectedAccountId);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            balance = rdr.GetDecimal(1);
                            return rdr.GetInt32(0);
                        }
                        else
                        {
                            Console.WriteLine("\nInvalid AccountID. Please try again.");
                        }
                    }
                }
            }
        }
    }

    // Handles depositing money into a selected account
    static void DepositMoney(int userId)
    {
        try
        {
            int accountId = SelectAccount(userId, out decimal balance);
            if (accountId == -1) return; // Exit if no valid account selected

            Console.Write("Enter amount to deposit: ");
         
            decimal amount = PromptForPositiveAmount(); // Get the deposit amount

            // Update account balance and log the transaction
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open(); // Open database connection
                SqlCommand command = new SqlCommand("UPDATE Accounts SET Balance = Balance + @Amount OUTPUT INSERTED.Balance WHERE UserID = @UserID AND AccountID = @AccountID", connection);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@AccountID", accountId);

                decimal newBalance = (decimal)command.ExecuteScalar(); // Execute command and get new balance
                Console.WriteLine($"\nDeposit successful. New balance: R {newBalance} \n");

                LogTransaction(connection, accountId, "Deposit", amount); // Log the deposit transaction
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during deposit: {ex.Message}");
        }
    }

    // Handles withdrawing money from a selected account
    static void WithdrawMoney(int userId)
    {
        try
        {
            int accountId = SelectAccount(userId, out decimal balance);
            if (accountId == -1) return; // Exit if no valid account selected

            Console.Write("Enter amount to withdraw: ");
            Console.Clear();
            Console.WriteLine(title);
            decimal amount = PromptForPositiveAmount(); // Get the withdrawal amount

            // Check if there is sufficient balance
            if (balance < amount)
            {
                Console.WriteLine("\n Oopsie, you have Insufficient balance.\n");
                return;
            }

            // Update account balance and log the transaction
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open(); // Open database connection
                SqlCommand command = new SqlCommand("UPDATE Accounts SET Balance = Balance - @Amount OUTPUT INSERTED.Balance WHERE UserID = @UserID AND AccountID = @AccountID", connection);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@AccountID", accountId);

                decimal newBalance = (decimal)command.ExecuteScalar(); // Execute command and get new balance
                Console.WriteLine($"\nWithdrawal successful. New balance: R {newBalance}\n");

                LogTransaction(connection, accountId, "Withdrawal", amount); // Log the withdrawal transaction
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during withdrawal: {ex.Message}");
        }
    }

    // Prompts the user to enter a positive amount and validates the input
    static decimal PromptForPositiveAmount()
    {
        decimal amount;
        while (true)
        {
            if (decimal.TryParse(Console.ReadLine(), out amount) && amount > 0)
            {
                return amount; // Return valid amount
            }
            Console.WriteLine("\nInvalid amount. It must be a positive number. Please try again.\n");
        }
    }

    // Displays the current balance of a selected account
    static void ViewBalance(int userId)
    {
        try
        {
            int accountId = SelectAccount(userId, out decimal balance);
            if (accountId == -1) return; // Exit if no valid account selected

            Console.WriteLine($"\nCurrent balance: R {balance}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nAn error occurred while viewing balance: {ex.Message}");
        }
    }

    // Displays past transactions for a selected account
    static void ViewPastTransactions(int userId)
    {
        try
        {
            int accountId = SelectAccount(userId, out decimal balance);
            if (accountId == -1) return; // Exit if no valid account selected

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open(); // Open database connection
                SqlCommand command = new SqlCommand("SELECT TransactionType, Amount, TransactionDateTime FROM Transactions WHERE AccountID = @AccountID ORDER BY TransactionDateTime DESC", connection);
                command.Parameters.AddWithValue("@AccountID", accountId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        // Display past transactions
                        while (reader.Read())
                        {
                            string transactionType = reader.GetString(0);
                            decimal amount = reader.GetDecimal(1);
                            DateTime dateTime = reader.GetDateTime(2);

                            Console.WriteLine($"\n{dateTime}: {transactionType} - {amount:C}\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine("\nNo past transactions found for this account.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nAn error occurred while viewing past transactions: {ex.Message}");
        }
    }

    // Logs a transaction into the database
    static void LogTransaction(SqlConnection connection, int accountId, string transactionType, decimal amount)
    {
        try
        {
            SqlCommand command = new SqlCommand("INSERT INTO Transactions (AccountID, TransactionType, Amount, TransactionDateTime) VALUES (@AccountID, @TransactionType, @Amount, @TransactionDateTime)", connection);
            command.Parameters.AddWithValue("@AccountID", accountId);
            command.Parameters.AddWithValue("@TransactionType", transactionType);
            command.Parameters.AddWithValue("@Amount", amount);
            command.Parameters.AddWithValue("@TransactionDateTime", DateTime.Now);

            command.ExecuteNonQuery(); // Execute the command to log the transaction
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while logging the transaction: {ex.Message}");
        }
    }
}