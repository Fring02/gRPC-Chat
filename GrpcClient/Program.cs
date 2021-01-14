using ChatClient;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Threading.Tasks;

namespace GrpcClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Write("Please enter your name: ");
            var username = Console.ReadLine();
            var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new ChatService.ChatServiceClient(channel);
            foreach(var chatRoom in client.GetChatRoomsAsync(new Lookup()).ResponseAsync.Result.ChatRooms_)
            {
                Console.WriteLine(chatRoom.Id + ": " + chatRoom.Name);
            }
            using (var chat = client.JoinAndWriteMessage())
            {
                _ = Task.Run(async () =>
                {
                    while (await chat.ResponseStream.MoveNext())
                    {
                        var response = chat.ResponseStream.Current;
                        Console.WriteLine($"{response.User} : {response.Text}");
                    }
                });
                Console.WriteLine("Enter id of chat room to join");
                var preferredChatRoom = Console.ReadLine();
                if (!string.IsNullOrEmpty(preferredChatRoom))
                {
                    Console.Clear();
                    await chat.RequestStream.WriteAsync(new Message { User = username, ChatRoomId = preferredChatRoom });
                }
                string messageText;
                while ((messageText = Console.ReadLine()) != null)
                {
                    if (messageText.ToUpper() == "EXIT")
                    {
                        break;
                    }
                    await chat.RequestStream.WriteAsync(new Message { User = username, Text = messageText, ChatRoomId = preferredChatRoom });
                }

                await chat.RequestStream.CompleteAsync();
            }

            Console.WriteLine("Disconnection started!");
            await channel.ShutdownAsync();
        }
    }
}
