﻿using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Lightest.TestingService.Interfaces;
using Lightest.TestingService.Models;

namespace Lightest.TestingService.Services
{
    public class TransferService : ITransferService
    {
        private readonly IPEndPoint _endpoint;

        public TransferService(IPAddress ip, int port)
        {
            _endpoint = new IPEndPoint(ip, port);
        }

        public async Task<bool> SendFile(string path, FileRequestType fileType)
        {
            if (!File.Exists(path))
            {
                return false;
            }
            var name = Path.GetFileName(path);
            var fileRequest = new FileRequest
            {
                Filename = name,
                RequestType = fileType
            };
            var result = await SendMessage($"name:{name}");
            if (!result)
            {
                return false;
            }
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(_endpoint.Address, _endpoint.Port);
                if (!client.Connected)
                {
                    return false;
                }
                using (var netStream = client.GetStream())
                using (var writer = new BinaryWriter(netStream))
                using (var fileStream = File.OpenRead(path))
                {
                    var length = fileStream.Length;
                    //+1 because type is written
                    writer.Write(length + 1);
                    //todo: to enum
                    writer.Write(2);
                    await fileStream.CopyToAsync(netStream);
                }
            }
            return true;
        }

        public async Task<bool> SendFile(string filename, FileRequestType fileType, byte[] data)
        {
            var result = await SendMessage($"name:{filename}");
            if (!result)
            {
                return false;
            }
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(_endpoint.Address, _endpoint.Port);
                if (!client.Connected)
                {
                    return false;
                }
                using (var netStream = client.GetStream())
                using (var writer = new BinaryWriter(netStream))
                {
                    var length = data.Length;
                    //+1 because type is written
                    writer.Write(length + 1);
                    //todo: to enum
                    writer.Write(2);
                    writer.Write(data);
                }
            }
            return true;
        }

        public async Task<bool> SendMessage(string message)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(_endpoint.Address, _endpoint.Port);
                if (!client.Connected)
                {
                    return false;
                }
                using (var stream = client.GetStream())
                using (var writer = new BinaryWriter(stream))
                {
                    long length = message.Length + 1;
                    writer.Write(length);
                    writer.Write(1);
                    var bytes = Encoding.UTF8.GetBytes(message);
                    writer.Write(bytes);
                }
            }
            return true;
        }
    }
}
