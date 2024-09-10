using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

class Program
{
    // Database connection string
    static string connectionString = @"Data Source=DESKTOP-6RT5AA5;Initial Catalog=SecureVault;Integrated Security=True;Encrypt=False";
    static string title;

    static void Main(string[] args)
    {
        // Set console text color
        Console.ForegroundColor = ConsoleColor.DarkCyan;

        // Display the app title and login screen
        DisplayTitle();
        Login();
    }

    // Method to display the application title with centered formatting
    static void DisplayTitle()
    {
        Console.Clear(); // Clear the console before displaying title
        title = @".·:''''''''''''''''''''''''''''''''''''''''''''''''''''''''':·.
: :  ____                         __     __          _ _    : :
: : / ___|  ___  ___ _   _ _ __ __\ \   / /_ _ _   _| | |_  : :
: : \___ \ / _ \/ __| | | | '__/ _ \ \ / / _` | | | | | __| : :
: :  ___) |  __/ (__| |_| | | |  __/\ V / (_| | |_| | | |_  : :
: : |____/ \___|\___|\__,_|_|  \___| \_/ \__,_|\__,_|_|\__| : :
'·:.........................................................:·'";

        // Center the title text based on the console window width
        int consoleWidth = Console.WindowWidth;
        string[] lines = title.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        foreach (string line in lines)
        {
            int padding = (consoleWidth - line.Length) / 2;
            Console.WriteLine(new string(' ', padding) + line);
        }
    }

    // Method to handle user login
    static void Login()
    {
        bool isLoggedIn = false;

        while (!isLoggedIn)
        {
            try
            {
                // Prompt for User ID
                Console.Write("\nEnter your User ID: ");
                if (!int.TryParse(Console.ReadLine(), out int userId) || userId <= 0)
                {
                    // Validate that User ID is a positive integer
                    Console.WriteLine("Invalid User ID. It must be a positive integer.");
                    continue;
                }

                // Prompt for PIN and mask the input
                Console.Write("Enter your PIN: ");
                string enteredPIN = ReadPIN();
                Console.Clear();
                Console.WriteLine(title);

                if (string.IsNullOrWhiteSpace(enteredPIN))
                {
                    // Ensure the PIN is not empty or whitespace
                    Console.WriteLine("PIN cannot be empty or whitespace.");
                    continue;
                }

                // Open the database connection
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Prepare SQL query to check User ID and PIN
                    SqlCommand command = new SqlCommand("SELECT UserID, UserName FROM Users WHERE UserID = @UserID AND EncryptedPIN = @PIN", connection);
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@PIN", enteredPIN); // PIN is not hashed in this version

                    // Execute the query and check if user exists
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Successful login
                            string userName = reader.GetString(1);
                            Console.WriteLine($"\nWelcome, {userName}!\n");
                            isLoggedIn = true;
                            ShowMenu(userId); // Show the main menu after login
                        }
                        else
                        {
                            // Invalid login
                            Console.WriteLine("Invalid User ID or PIN. Please try again.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any errors during login
                Console.WriteLine($"An error occurred during login: {ex.Message}");
            }
        }
    }

    // Method to display the main menu options
    static void ShowMenu(int userId)
    {
        bool exit = false;
        while (!exit)
        {
            // Display available menu options
            Console.WriteLine("1. Deposit Money");
            Console.WriteLine("2. Withdraw Money");
            Console.WriteLine("3. View Balance");
            Console.WriteLine("4. View Past Transactions");
            Console.WriteLine("5. Exit");
            Console.Write("Select from the above menu: ");

            // Get the user's choice
            string choice = Console.ReadLine();
            Console.Clear();
            Console.WriteLine(title);

            // Execute the selected option
            switch (choice)
            {
                case "1":
                    DepositMoney(userId);
                    break;
                case "2":
                    WithdrawMoney(userId);
                    break;
                case "3":
                    ViewBalance(userId);
                    break;
                case "4":
                    ViewPastTransactions(userId);
                    break;
                case "5":
                    Login(); // Restart the login process on exit
                    break;
                default:
                    // Handle invalid menu choices
                    Console.WriteLine("\nInvalid choice. Please select a valid option.");
                    break;
            }
        }
    }

    // Method to read PIN input from the user while masking it with asterisks
    static string ReadPIN()
    {
        StringBuilder pin = new StringBuilder();
        ConsoleKey key;

        // Loop to read each character of the PIN
        do
        {
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;

            if (key == ConsoleKey.Backspace && pin.Length > 0)
            {
                // Handle backspace, remove the last character and asterisk
                pin.Remove(pin.Length - 1, 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                // Append the character to the PIN and display an asterisk
                pin.Append(keyInfo.KeyChar);
                Console.Write("*");
            }
        } while (key != ConsoleKey.Enter); // Continue until Enter is pressed

        Console.WriteLine();
        return pin.ToString(); // Return the entered PIN
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
                    Console.WriteLine("\nAvailable Accounts:\n");
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
            decimal amount = PromptForPositiveAmount(); // Get the withdrawal amount
            Console.Clear();
            Console.WriteLine(title);

            // Check if there is sufficient balance
            if (balance < amount)
            {
                Console.WriteLine("\n Oopsie, you have Insufficient funds.\n");
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