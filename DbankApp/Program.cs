using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Net.Http.Headers;
//Ziyu Ma 8865319 
namespace DbankApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a new database connection:
            SQLiteConnection dataBase = new SQLiteConnection("Data Source=Dbank.db;Version=3;New=True;Compress=True;");
            // Open the connection:
            try
            {
                dataBase.Open();
                CreateTables(dataBase);
                bool quit = false;
                while (!quit)
                {
                    Console.WriteLine("Welcome to MABJ Bank! Please choose from 1-6: ");
                    Console.WriteLine("1. Create bank account");
                    Console.WriteLine("2. Perform account deposit");
                    Console.WriteLine("3. Perform account withdrawal");
                    Console.WriteLine("4. Transfer money");
                    Console.WriteLine("5. Check account balance");
                    Console.WriteLine("6. Quit application");
                    int choice = Convert.ToInt32(Console.ReadLine());
                    switch (choice)
                    {
                        case 1:
                            CreateAccount(dataBase);
                            break;
                        case 2:
                            Deposit(dataBase);
                            break;
                        case 3:
                            Withdraw(dataBase);
                            break;
                        case 4:
                            Transfer(dataBase);
                            break;
                        case 5:
                            CheckBalance(dataBase);
                            break;
                        case 6:
                            quit = true;
                            break;
                        default:
                            Console.WriteLine("Invalid choice.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
            }
            dataBase.Close();
        }

        static void CreateTables(SQLiteConnection db)
        {
            SQLiteCommand dataCmd = db.CreateCommand();

            // create customers table
            dataCmd.CommandText = "CREATE TABLE IF NOT EXISTS customers (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL);";
            dataCmd.ExecuteNonQuery();

            // create accounts table
            dataCmd.CommandText = "CREATE TABLE IF NOT EXISTS accounts (id INTEGER PRIMARY KEY AUTOINCREMENT, customer_id INTEGER NOT NULL REFERENCES customers(id), balance REAL DEFAULT 0);";
            dataCmd.ExecuteNonQuery();

            // create transactions table
            dataCmd.CommandText = "CREATE TABLE IF NOT EXISTS transactions (id INTEGER PRIMARY KEY AUTOINCREMENT, type TEXT NOT NULL CHECK(type IN ('deposit', 'withdraw', 'transfer')), amount REAL NOT NULL CHECK(amount >= 0), from_account_id INTEGER REFERENCES accounts(id), to_account_id INTEGER REFERENCES accounts(id));";
            dataCmd.ExecuteNonQuery();
        }

        static void CreateAccount(SQLiteConnection db)
        {
            SQLiteCommand dataCmd = db.CreateCommand();

            // get customer name
            Console.Write("Please enter your name: ");
            string name = Console.ReadLine();

            // insert customer into database
            dataCmd.CommandText = "INSERT INTO customers(name) VALUES(@name);";
            dataCmd.Parameters.AddWithValue("@name", name);
            int result = dataCmd.ExecuteNonQuery();

            if (result == 1) { 
            long customerId = db.LastInsertRowId;
            dataCmd.CommandText = "INSERT INTO accounts(customer_id) VALUES(@customerId);";
            dataCmd.Parameters.AddWithValue("@customerId", customerId);
            result = dataCmd.ExecuteNonQuery();

            if (result == 1)
                {
                    long accountId = db.LastInsertRowId;
                    ConsoleColor originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Thank you {name}! Your account has been created. Please remember your account number: {accountId}.");
                    Console.ForegroundColor = originalColor;
                }
                
            }
        }

        static void Deposit(SQLiteConnection db)
        {
            SQLiteCommand dataCmd = db.CreateCommand();

            // get account id and deposit amount
            Console.Write("Please enter your account number: ");
            int accountId = Convert.ToInt32(Console.ReadLine());
            Console.Write("How much would you like to deposit? ");
            double amount = Convert.ToDouble(Console.ReadLine());

            //Validate amount input
            if (amount < 0) { 
                Console.WriteLine("Amount must be positive.");}
            else { 

            // update account balance in database
            dataCmd.CommandText = "UPDATE accounts SET balance=balance+@amount WHERE id=@accountId;";
            dataCmd.Parameters.AddWithValue("@amount", amount);
            dataCmd.Parameters.AddWithValue("@accountId", accountId);
            int result = dataCmd.ExecuteNonQuery();

            if (result == 1)
            {
                // insert transaction into database
                dataCmd.CommandText = "INSERT INTO transactions(type, amount, to_account_id) VALUES('deposit', @amount, @accountId);";
                dataCmd.Parameters.AddWithValue("@amount", amount);
                dataCmd.Parameters.AddWithValue("@accountId", accountId);
                dataCmd.ExecuteNonQuery();
                    ConsoleColor originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine($"You have deposit ${amount} to your account number: {accountId}.");
                    Console.ForegroundColor = originalColor;
                }
            else
            {
                Console.WriteLine("Invalid account number.");
            }
            }
        }

        static void Withdraw(SQLiteConnection db)
        {
            SQLiteCommand dataCmd = db.CreateCommand();

            // get account id and withdraw amount
            Console.Write("Please enter your account number: ");
            int accountId = Convert.ToInt32(Console.ReadLine());
            Console.Write("How much would you like to withdraw? ");
            double amount = Convert.ToDouble(Console.ReadLine());

            if (amount < 0) { 
                Console.WriteLine("Amount must be positive.");}
            else { 

            // validate sufficient balance available
            dataCmd.CommandText = "SELECT balance FROM accounts WHERE id=@accountId;";
            dataCmd.Parameters.AddWithValue("@accountId", accountId);
            SQLiteDataReader reader = dataCmd.ExecuteReader();
            if (reader.Read())
            {
                double balance = reader.GetDouble(0);
                if (balance < amount) { 
                    Console.WriteLine("You don't have enough money in your account.");
                }
                else { 

                // update account balance in database
                reader.Close();
                dataCmd.CommandText = "UPDATE accounts SET balance=balance-@amount WHERE id=@accountId;";
                dataCmd.Parameters.AddWithValue("@amount", amount);
                int result = dataCmd.ExecuteNonQuery();

                if (result == 1)
                {
                    // insert transaction into database
                    dataCmd.CommandText = "INSERT INTO transactions(type, amount, from_account_id) VALUES('withdraw', @amount, @accountId);";
                    dataCmd.Parameters.AddWithValue("@amount", amount);
                    dataCmd.Parameters.AddWithValue("@accountId", accountId);
                    dataCmd.ExecuteNonQuery();
                    ConsoleColor originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine($"You withdrawaled ${amount} from your account number: {accountId}.");
                            Console.ForegroundColor = originalColor;
                        }
                else
                {
                    Console.WriteLine("Account can not be found.");
                }
                }
            }
            else
            {
                reader.Close();
                Console.WriteLine("Account can not be found.");
            }
            }
        }

        static void Transfer(SQLiteConnection db)
        {
            SQLiteCommand dataCmd = db.CreateCommand();

            // get from and to account ids and transfer amount
            Console.Write("You want to transfer from which account? ");
            int fromAccountId = Convert.ToInt32(Console.ReadLine());
            Console.Write("You want to transfer to which account? ");
            int toAccountId = Convert.ToInt32(Console.ReadLine());
            Console.Write("How much do you want to transfer: ");
            double amount = Convert.ToDouble(Console.ReadLine());

            if (amount < 0) { 
                Console.WriteLine("Amount must be positive.");}
            else { 

            // validate sufficient balance available in from account
            dataCmd.CommandText = "SELECT balance FROM accounts WHERE id=@fromAccountId;";
            dataCmd.Parameters.AddWithValue("@fromAccountId", fromAccountId);
            SQLiteDataReader reader = dataCmd.ExecuteReader();
            if (reader.Read())
            {
                double balance = reader.GetDouble(0);
                if (balance < amount) { 
                    Console.WriteLine("You don't have enough money in your account.");}
                else { 
                    

                // update balances in database
                reader.Close();
                dataCmd.CommandText = "UPDATE accounts SET balance=balance-@amount WHERE id=@fromAccountId; UPDATE accounts SET balance=balance+@amount WHERE id=@toAccountId;";
                dataCmd.Parameters.AddWithValue("@amount", amount);
                dataCmd.Parameters.AddWithValue("@toAccountId", toAccountId);
                int result = dataCmd.ExecuteNonQuery();

                if (result == 2)
                {
                    // insert transaction into database
                    dataCmd.CommandText = "INSERT INTO transactions(type, amount, from_account_id, to_account_id) VALUES('transfer', @amount, @fromAccountId, @toAccountId);";
                    dataCmd.Parameters.AddWithValue("@amount", amount);
                    dataCmd.Parameters.AddWithValue("@fromAccountId", fromAccountId);
                    dataCmd.Parameters.AddWithValue("@toAccountId", toAccountId);
                    dataCmd.ExecuteNonQuery();
                    ConsoleColor originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"You have transfered ${amount} from account {fromAccountId} to account {toAccountId}.");
                            Console.ForegroundColor = originalColor;
                        }
                else
                {
                    Console.WriteLine("Account can not be found.");
                }
                    }
                }
            else
            {
                reader.Close();
                Console.WriteLine("Account can not be found.");
            }
            }
        }

        static void CheckBalance(SQLiteConnection db)
        {
            SQLiteCommand dataCmd = db.CreateCommand();

            // get account id
            Console.Write("Please enter your account number: ");
            int accountId = Convert.ToInt32(Console.ReadLine());

            // get balance from database
            dataCmd.CommandText = "SELECT balance FROM accounts WHERE id=@accountId;";
            dataCmd.Parameters.AddWithValue("@accountId", accountId);
            SQLiteDataReader reader = dataCmd.ExecuteReader();
            if (reader.Read())
            {
                double balance = reader.GetDouble(0);
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Your account {accountId} has a balance of ${balance}.");
                Console.ForegroundColor = originalColor;
                reader.Close();
            }
            else
            {
                reader.Close();
                Console.WriteLine("Account can not be found.");
            }
        }
    }
}


