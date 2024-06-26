﻿using System;
using MySql.Data.MySqlClient;
using Rocket.Core.Logging;

namespace Uconomy
{
    public class DatabaseMgr
    {
        private readonly UconomyPlugin _uconomy;

        internal DatabaseMgr(UconomyPlugin uconomy)
        {
            _uconomy = uconomy;
            CheckSchema();
        }

        private void CheckSchema()
        {
            try
            {
                MySqlConnection mySqlConnection = CreateConnection();
                MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
                mySqlConnection.Open();
                mySqlCommand.CommandText = string.Concat(
                "CREATE TABLE IF NOT EXISTS `",
                _uconomy.Configuration.Instance.UconomyTableName,
                "` (",
                "`steamId` VARCHAR(32) NOT NULL,",
                "`balance` DOUBLE NOT NULL,",
                "`lastUpdated` VARCHAR(32) NOT NULL,",
                "PRIMARY KEY (`steamId`)",
                ");"
            );
                mySqlCommand.ExecuteNonQuery();
                mySqlConnection.Close();
            }
            catch (Exception exception)
            {
                Logger.LogError($"[Uconomy] Database Crashed by Console when trying to create or check existing table {_uconomy.Configuration.Instance.UconomyTableName}, reason: {exception.Message}");
            }
        }

        public MySqlConnection CreateConnection()
        {
            MySqlConnection mySqlConnection = null;
            try
            {
                mySqlConnection = new MySqlConnection(string.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3};PORT={4};", _uconomy.Configuration.Instance.DatabaseAddress, _uconomy.Configuration.Instance.DatabaseName, _uconomy.Configuration.Instance.DatabaseUsername, _uconomy.Configuration.Instance.DatabasePassword, _uconomy.Configuration.Instance.DatabasePort));
            }
            catch (Exception exception)
            {
                Logger.LogError($"[Uconomy] Database Crashed, reason: {exception.Message}");
            }
            return mySqlConnection;
        }

        /// <summary>
        /// Add a new player to the uconomy database if not exist
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="balance"></param>
        public void AddNewPlayer(string playerId, decimal balance)
        {
            try
            {
                // Instanciate connection
                MySqlConnection mySqlConnection = CreateConnection();
                // Instanciate command
                MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
                // Command: Insert new player only if not exist the same steamId
                mySqlCommand.CommandText = string.Concat("Insert ignore into `", _uconomy.Configuration.Instance.UconomyTableName, "` (`steamId`, `balance`, `lastUpdated`) VALUES ('", playerId, "', '", balance, "', '", DateTime.Now.ToShortDateString(), "');");
                // Try to connect
                mySqlConnection.Open();
                // Execute the command
                mySqlCommand.ExecuteNonQuery();
                // Close connection
                mySqlConnection.Close();
            }
            catch (Exception exception)
            {
                Logger.LogError($"[Uconomy] Database Crashed by {playerId} from function AddNewPlayer, reason: {exception.Message}");
            }
        }

        /// <summary>
        /// Returns the decimal player balance from the table uconomy
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public decimal GetBalance(string playerId)
        {
            decimal num = new(0);
            try
            {
                MySqlConnection mySqlConnection = CreateConnection();
                MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
                mySqlCommand.CommandText = string.Concat("select `balance` from `", _uconomy.Configuration.Instance.UconomyTableName, "` where `steamId` = '", playerId, "';");
                mySqlConnection.Open();
                object obj = mySqlCommand.ExecuteScalar();
                if (obj != null)
                {
                    decimal.TryParse(obj.ToString(), out num);
                }
                mySqlConnection.Close();
            }
            catch (Exception exception)
            {
                Logger.LogError($"[Uconomy] Database Crashed by {playerId} from function GetBalance, reason: {exception.Message}");
            }
            return num;
        }

        /// <summary>
        /// Make a pay query from other player, returns true if successfuly payed
        /// </summary>
        /// <param name="payingPlayerId"></param>
        /// <param name="receivedPlayerId"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool PlayerPayPlayer(string payingPlayerId, string receivedPlayerId, decimal amount)
        {
            try
            {
                decimal payingPlayerBalance = GetBalance(payingPlayerId);
                if ((payingPlayerBalance - amount) < 0)
                {
                    return false;
                }

                RemoveBalance(payingPlayerId, amount);
                AddBalance(receivedPlayerId, amount);
                return true;
            }
            catch (Exception exception)
            {
                Logger.LogError($"[Uconomy] Database Crashed by {payingPlayerId} and {receivedPlayerId} from function PlayerPayPlayer, reason: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove a balance from the player
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cost"></param>
        public void RemoveBalance(string id, decimal cost)
        {
            try
            {
                MySqlConnection mySqlConnection = CreateConnection();
                MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
                mySqlCommand.CommandText = $"update `{_uconomy.Configuration.Instance.UconomyTableName}` set `balance` = `balance` - {cost} where `steamId` = {id};";
                mySqlConnection.Open();
                mySqlCommand.ExecuteNonQuery();
                mySqlConnection.Close();
            }
            catch (Exception exception)
            {
                Logger.LogError($"[Uconomy] Database Crashed by {id} from function RemoveBalance, reason: {exception.Message}");
            }
        }

        /// <summary>
        /// Add more balance to the player
        /// </summary>
        /// <param name="id"></param>
        /// <param name="quantity"></param>
        public void AddBalance(string id, decimal quantity)
        {
            try
            {
                MySqlConnection mySqlConnection = CreateConnection();
                MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
                mySqlCommand.CommandText = $"update `{_uconomy.Configuration.Instance.UconomyTableName}` set `balance` = `balance` + {quantity} where `steamId` = {id};";
                mySqlConnection.Open();
                mySqlCommand.ExecuteNonQuery();
                mySqlConnection.Close();
            }
            catch (Exception exception)
            {
                Logger.LogError($"[Uconomy] Database Crashed by {id} from function AddBalance, reason: {exception.Message}");
            }
        }
    }
}