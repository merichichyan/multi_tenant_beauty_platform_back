using System;
using Npgsql;

class CheckDbScript
{
    public static void Run()
    {
        string connString = "Host=ep-red-dream-at8tosch.c-9.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_GpLPRt1ej4iQ;SslMode=Require;Trust Server Certificate=true";
        try
        {
            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            Console.WriteLine("Successfully connected to the database!");

            Console.WriteLine("\n--- USERS IN DATABASE ---");
            using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"FullName\", \"Email\", \"Role\" FROM \"Users\"", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"Id: {reader["Id"]}, Name: {reader["FullName"]}, Email: {reader["Email"]}, Role: {reader["Role"]}");
                }
            }

            Console.WriteLine("\n--- SPECIALISTS IN DATABASE ---");
            using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"FullName\" FROM \"Specialists\"", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"Id: {reader["Id"]}, Name: {reader["FullName"]}");
                }
            }

            Console.WriteLine("\n--- SERVICE ITEMS IN DATABASE ---");
            using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"Name\", \"Category\", \"Price\", \"SpecialistId\", \"StaffMemberId\" FROM \"ServiceItems\"", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"Id: {reader["Id"]}, Name: {reader["Name"]}, Cat: {reader["Category"]}, Price: {reader["Price"]}, SpecialistId: {reader["SpecialistId"]}, StaffMemberId: {reader["StaffMemberId"]}");
                }
            }

            Console.WriteLine("\n--- SALONS IN DATABASE ---");
            using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"SalonName\" FROM \"Salons\"", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"Id: {reader["Id"]}, SalonName: {reader["SalonName"]}");
                }
            }

            Console.WriteLine("\n--- STAFF MEMBERS IN DATABASE ---");
            using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"SalonId\", \"FullName\", \"SpecialistId\" FROM \"StaffMembers\"", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"Id: {reader["Id"]}, SalonId: {reader["SalonId"]}, Name: {reader["FullName"]}, SpecialistId: {reader["SpecialistId"]}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
