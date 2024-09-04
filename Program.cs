using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

class Program
{
    // Connection string to the database
    static string connectionString = @"Data Source=DESKTOP-6RT5AA5;Initial Catalog=SecureVault;Integrated Security=True;Encrypt=False";

    static void Main(string[] args)
    {
        // Set the console text color to Dark Cyan
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(@".·:''''''''''''''''''''''''''''''''''''''''''''''''''''''''':·.
: :  ____                         __     __          _ _    : :
: : / ___|  ___  ___ _   _ _ __ __\ \   / /_ _ _   _| | |_  : :
: : \___ \ / _ \/ __| | | | '__/ _ \ \ / / _` | | | | | __| : :
: :  ___) |  __/ (__| |_| | | |  __/\ V / (_| | |_| | | |_  : :
: : |____/ \___|\___|\__,_|_|  \___| \_/ \__,_|\__,_|_|\__| : :
'·:.........................................................:·'");

        // Call the Login method to authenticate the user
        Login();
    }

    // Method to handle user login
    static void Login()
    {
        try
        {
            Console.Write("\nEnter your User ID: ");
            if (!int.TryParse(Console.ReadLine(), out int userId) || userId <= 0)
            {
                Console.WriteLine("Invalid User ID. It must be a positive integer.");
                return;
            }

            Console.Write("Enter your PIN: ");
            string enteredPIN = Console.ReadLine();
            Console.Clear();

            if (string.IsNullOrWhiteSpace(enteredPIN))
            {
                Console.WriteLine("PIN cannot be empty or whitespace.");
                return;
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT UserID, UserName FROM Users WHERE UserID = @UserID AND EncryptedPIN = @PIN", connection);
                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@PIN", enteredPIN);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string userName = reader.GetString(1);
                        Console.WriteLine($"\nWelcome, {userName}!\n");
                        ShowMenu(userId);
                    }
                    else
                    {
                        Console.WriteLine("Invalid User ID or PIN. Access Denied.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during login: {ex.Message}");
        }
    }

    static void ShowMenu(int userId)
    {
        bool exit = false;
        while (!exit)
        {
            Console.WriteLine("1. Deposit Money");
            Console.WriteLine("2. Withdraw Money");
            Console.WriteLine("3. View Balance");
            Console.WriteLine("4. View Past Transactions");
            Console.WriteLine("5. Exit");

            string choice = Console.ReadLine();
            Console.Clear();
            Console.WriteLine("\n" + "WHICH ACCOUNT YOU WANT TO USE? ");

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
                    exit = true;
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please select a valid option.");
                    break;
            }
        }
    }

    static int SelectAccount(int userId, out decimal balance)
    {
        balance = 0;
        List<int> accountIds = new List<int>();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            // Retrieve and display all accounts
            string query = "SELECT AccountID, AccountType, AccountNumber FROM Accounts WHERE UserID = @UserID";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserID", userId);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        Console.WriteLine("***************No accounts found.***************");
                        return -1;
                    }

                    Console.WriteLine("__________________________________________________________________\n");
                    while (reader.Read())
                    {
                        int accountId = reader.GetInt32(0);
                        string accountType = reader.GetString(1);
                        string accountNumber = reader.GetString(2);
                        accountIds.Add(accountId);
                        Console.WriteLine($"AccountID: {accountId}, AccountType: {accountType}, AccountNumber: {accountNumber}");
                    }
                    Console.WriteLine("__________________________________________________________________");
                }
            }

            while (true)
            {
                // Prompt user for AccountID
                Console.Write("\nEnter AccountID: ");
                if (!int.TryParse(Console.ReadLine(), out int selectedAccountId) || !accountIds.Contains(selectedAccountId))
                {
                    Console.WriteLine("***************Invalid AccountID. Please enter a valid AccountID from the list.***************");
                    continue; // Ask for AccountID again
                }

                // Verify AccountID
                query = "SELECT AccountID, Balance FROM Accounts WHERE AccountID = @AccountID";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@AccountID", selectedAccountId);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            balance = rdr.GetDecimal(1);
                            return rdr.GetInt32(0); // Return the valid AccountID
                        }
                        else
                        {
                            Console.WriteLine("***************Invalid AccountID. Please try again.***************");
                        }
                    }
                }
            }
        }
    }

    static void DepositMoney(int userId)
    {
        try
        {
            int accountId = SelectAccount(userId, out decimal balance);
            if (accountId == -1) return;

            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();
            Console.Clear();

            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                Console.WriteLine("Account number cannot be empty or whitespace.");
                return;
            }

            decimal amount = PromptForPositiveAmount("Enter amount to deposit: ");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("UPDATE Accounts SET Balance = Balance + @Amount OUTPUT INSERTED.Balance WHERE UserID = @UserID AND AccountNumber = @AccountNumber", connection);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);

                decimal newBalance = (decimal)command.ExecuteScalar();
                Console.WriteLine($"***********Deposit successful. New balance: R {newBalance}***********");

                LogTransaction(connection, accountNumber, "Deposit", amount);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during deposit: {ex.Message}");
        }
    }

    static void WithdrawMoney(int userId)
    {
        try
        {
            int accountId = SelectAccount(userId, out decimal balance);
            if (accountId == -1) return;

            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                Console.WriteLine("Account number cannot be empty or whitespace.");
                return;
            }

            decimal amount = PromptForPositiveAmount("Enter amount to withdraw: ");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand checkBalanceCommand = new SqlCommand("SELECT Balance FROM Accounts WHERE UserID = @UserID AND AccountNumber = @AccountNumber", connection);
                checkBalanceCommand.Parameters.AddWithValue("@UserID", userId);
                checkBalanceCommand.Parameters.AddWithValue("@AccountNumber", accountNumber);

                decimal currentBalance = (decimal)checkBalanceCommand.ExecuteScalar();
                if (currentBalance >= amount)
                {
                    SqlCommand command = new SqlCommand("UPDATE Accounts SET Balance = Balance - @Amount OUTPUT INSERTED.Balance WHERE UserID = @UserID AND AccountNumber = @AccountNumber", connection);
                    command.Parameters.AddWithValue("@Amount", amount);
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@AccountNumber", accountNumber);

                    decimal newBalance = (decimal)command.ExecuteScalar();
                    Console.WriteLine($"***********Withdrawal successful. New balance: R {newBalance}***********");

                    LogTransaction(connection, accountNumber, "Withdrawal", amount);
                }
                else
                {
                    Console.WriteLine("***************Insufficient balance.***************");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during withdrawal: {ex.Message}");
        }
    }

    static decimal PromptForPositiveAmount(string prompt)
    {
        decimal amount;
        while (true)
        {
            Console.Write(prompt);
            if (decimal.TryParse(Console.ReadLine(), out amount) && amount > 0)
            {
                break;
            }
            Console.WriteLine("Invalid amount. It must be a positive number. Please try again.");
        }
        return amount;
    }

    static void ViewBalance(int userId)
    {
        try
        {
            int accountId = SelectAccount(userId, out decimal balance);
            if (accountId == -1) return;

            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                Console.WriteLine("Account number cannot be empty or whitespace.");
                return;
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT Balance FROM Accounts WHERE UserID = @UserID AND AccountNumber = @AccountNumber", connection);
                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);

                object result = command.ExecuteScalar();
                if (result != null)
                {
                    balance = (decimal)result;
                    Console.WriteLine($"***************Current balance: R {balance}***************");
                }
                else
                {
                    Console.WriteLine("***************Invalid account number. Please try again.***************");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while viewing balance: {ex.Message}");
        }
    }

    static void ViewPastTransactions(int userId)
    {
        try
        {
            int accountId = SelectAccount(userId, out decimal balance);
            if (accountId == -1) return;

            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                Console.WriteLine("Account number cannot be empty or whitespace.");
                return;
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT TransactionType, Amount, TransactionDateTime FROM Transactions t JOIN Accounts a ON t.AccountID = a.AccountID WHERE a.UserID = @UserID AND a.AccountNumber = @AccountNumber ORDER BY TransactionDateTime DESC", connection);
                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            string transactionType = reader.GetString(0);
                            decimal amount = reader.GetDecimal(1);
                            DateTime dateTime = reader.GetDateTime(2);

                            Console.WriteLine($"{dateTime}: {transactionType} - {amount:C}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("***************No past transactions found for this account.***************");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while viewing past transactions: {ex.Message}");
        }
    }

    static void LogTransaction(SqlConnection connection, string accountNumber, string transactionType, decimal amount)
    {
        try
        {
            SqlCommand accountCommand = new SqlCommand("SELECT AccountID FROM Accounts WHERE AccountNumber = @AccountNumber", connection);
            accountCommand.Parameters.AddWithValue("@AccountNumber", accountNumber);

            object result = accountCommand.ExecuteScalar();
            if (result != null)
            {
                int accountId = (int)result;

                SqlCommand command = new SqlCommand("INSERT INTO Transactions (AccountID, TransactionType, Amount, TransactionDateTime) VALUES (@AccountID, @TransactionType, @Amount, @TransactionDateTime)", connection);
                command.Parameters.AddWithValue("@AccountID", accountId);
                command.Parameters.AddWithValue("@TransactionType", transactionType);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@TransactionDateTime", DateTime.Now);

                command.ExecuteNonQuery();
            }
            else
            {
                Console.WriteLine("***************Account not found while logging transaction.***************");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while logging the transaction: {ex.Message}");
        }
    }
}
