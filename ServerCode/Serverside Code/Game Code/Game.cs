using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using PlayerIO.GameLibrary;
using System.Drawing;

namespace BotWarsGame {
	public class Player : BasePlayer {
		public string Name;

        // The bots this player owns
        public List<Bot> OwnedBots = new List<Bot>();
	}

    // This attribute is used to identify the room type
    // When starting a room, you can specify room type - it matches this value
	[RoomType("GameRoom")]
	public class GameCode : Game<Player> {
        Dictionary<ulong, Bot> bots = new Dictionary<ulong, Bot>();

		// This method is called when an instance of your the game is created
		public override void GameStarted() {
			// anything you write to the Console will show up in the 
			// output window of the development server
			Console.WriteLine("Game is started: " + RoomId);

			// Broadcast game state 5 times per second
			AddTimer(delegate {
                foreach (Player player in Players) {
                    foreach (Bot bot in player.OwnedBots) {
                        // Broadcast bot state (position & health)
                        foreach (Player target in Players) {
                            if (target != player) {
                                Broadcast("UpdateBot", bot.BotID, bot.PositionX, bot.PositionY, bot.Health);
                            }
                        }
                    }
                }
			}, 200);
			
			// Debug Example:
			// Sometimes, it can be very usefull to have a graphical representation
			// of the state of your game.
			// An easy way to accomplish this is to setup a timer to update the
			// debug view every 250th second (4 times a second).
			AddTimer(delegate {
				// This will cause the GenerateDebugImage() method to be called
				// so you can draw a grapical version of the game state.
				RefreshDebugView(); 
			}, 250);
		}

		// This method is called when the last player leaves the room, and it's closed down.
		public override void GameClosed() {
			Console.WriteLine("RoomId: " + RoomId);
		}

		// This method is called whenever a player joins the game
		public override void UserJoined(Player player) {
            //player.Name = player.JoinData["Name"];
            player.Name = "RandomPlayer";

            // Send the player their own ID
            player.Send("SetID", player.Id);

			// this is how you broadcast a message to all players connected to the game
			Broadcast("UserJoined", player.Id, player.Name);

            // Inform the user of everyone else in the room, plus their bots
            foreach (Player p in Players) {
                if (p == player) continue;

                player.Send("UserJoined", p.Id, p.Name);

                // Notify new player of existing bots
                foreach (Bot bot in p.OwnedBots) {
                    player.Send("OnBotSpawned", p.Id, bot.BotID, bot.PositionX, bot.PositionY);
                }
            }
		}

		// This method is called when a player leaves the game
		public override void UserLeft(Player player) {
			Broadcast("UserLeft", player.Id);
		}

		// This method is called when a player sends a message into the server code
		public override void GotMessage(Player player, Message message) {
            switch (message.Type) {
                case "SpawnBot": {
                        // Player spawned a bot
                        ulong botID = message.GetULong(0);
                        float botPosX = message.GetFloat(1);
                        float botPosY = message.GetFloat(2);
                        Bot bot = new Bot(player, botID);
                        bot.PositionX = botPosX;
                        bot.PositionY = botPosY;
                        player.OwnedBots.Add(bot);

                        // Add bot to dictionary so we can later look up bots by ID
                        bots.Add(botID, bot);

                        // Broadcast spawn message to other players
                        foreach (Player pl in Players) {
                            if (pl == player) continue;

                            pl.Send("OnBotSpawned", pl.Id, botID, botPosX, botPosY);
                        }
                    }
                    break;
                case "UpdateBot": {
                        // Update one of the player's bots
                        ulong botID = message.GetULong(0);
                        float botPosX = message.GetFloat(1);
                        float botPosY = message.GetFloat(2);
                        if (bots.ContainsKey(botID)) {
                            Bot bot = bots[botID];
                            bot.PositionX = botPosX;
                            bot.PositionY = botPosY;
                        }
                    }
                    break;
                case "TakeDamage": {
                        // One bot damaged another
                        ulong destBotID = message.GetULong(0);

                        Console.WriteLine("Taking damage: " + destBotID);

                        if (bots.ContainsKey(destBotID)) {
                            Bot destBot = bots[destBotID];
                            destBot.Health -= 10;

                            // Check if the bot died
                            if (destBot.Health <= 0) {
                                Console.WriteLine("BotDied: " + destBotID);

                                // Remove bot from world
                                foreach (Player pl in Players) {
                                    if (pl.Id == destBot.OwnerID) {
                                        pl.OwnedBots.Remove(destBot);

                                        // Player is out of bots?
                                        if (pl.OwnedBots.Count == 0) {
                                            // Boot them from the game
                                            pl.Disconnect();
                                        }
                                        break;
                                    }
                                }
                                bots.Remove(destBot.BotID);

                                // Broadcast death message
                                Broadcast("BotDied", destBot.BotID);

                                // Send got kill message to player sending the damage message
                                player.Send("GotKill");
                            } else {
                                // Send new health amount to victim
                                foreach (Player pl in Players) {
                                    if (pl.Id == destBot.OwnerID) {
                                        pl.Send("TookDamage", destBot.BotID, destBot.Health);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
		}

		Point debugPoint;

		// This method get's called whenever you trigger it by calling the RefreshDebugView() method.
		public override System.Drawing.Image GenerateDebugImage() {
			// we'll just draw 400 by 400 pixels image with the current time, but you can
			// use this to visualize just about anything.
			var image = new Bitmap(400,400);
			using(var g = Graphics.FromImage(image)) {
				// fill the background
				g.FillRectangle(Brushes.Blue, 0, 0, image.Width, image.Height);

				// draw the current time
				g.DrawString(DateTime.Now.ToString(), new Font("Verdana",20F),Brushes.Orange, 10,10);

				// draw a dot based on the DebugPoint variable
				g.FillRectangle(Brushes.Red, debugPoint.X,debugPoint.Y,5,5);
			}
			return image;
		}

		// During development, it's very usefull to be able to cause certain events
		// to occur in your serverside code. If you create a public method with no
		// arguments and add a [DebugAction] attribute like we've down below, a button
		// will be added to the development server. 
		// Whenever you click the button, your code will run.
		[DebugAction("Play", DebugAction.Icon.Play)]
		public void PlayNow() {
			Console.WriteLine("The play button was clicked!");
		}

		// If you use the [DebugAction] attribute on a method with
		// two int arguments, the action will be triggered via the
		// debug view when you click the debug view on a running game.
		[DebugAction("Set Debug Point", DebugAction.Icon.Green)]
		public void SetDebugPoint(int x, int y) {
			debugPoint = new Point(x,y);
		}
	}

    public class Bot {
        public ulong BotID;
        public int OwnerID;

        public float PositionX = 0f;
        public float PositionY = 0f;

        public int Health = 100;

        public Bot(Player owner, ulong botID) {
            this.OwnerID = owner.Id;
            this.BotID = botID;
        }
    }
}
