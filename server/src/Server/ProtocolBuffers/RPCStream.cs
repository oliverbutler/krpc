using System;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class RPCStream : IStream<Request,Response>
    {
        // 1MB buffer
        internal const int bufferSize = 1 * 1024 * 1024;
        readonly IStream<byte,byte> stream;
        Request bufferedRequest;
        byte[] buffer = new byte[bufferSize];
        int offset;

        public RPCStream (IStream<byte,byte> stream)
        {
            this.stream = stream;
        }

        /// <summary>
        /// Returns true if there is a request waiting to be read. A Call to Read() will
        /// not throw NoRequestException if this returns true. Throws MalformedRequestException
        /// if a malformed request is received.
        /// </summary>
        public bool DataAvailable {
            get {
                try {
                    Poll ();
                    return true;
                } catch (NoRequestException) {
                    return false;
                }
            }
        }

        /// <summary>
        /// Read a request from the client. Blocks until a request is available.
        /// Throws NoRequestException if there is no request.
        /// Throws MalformedRequestException if malformed data is received.
        /// </summary>
        public Request Read ()
        {
            Poll ();
            var request = bufferedRequest;
            bufferedRequest = null;
            return request;
        }

        public int Read (Request[] buffer, int offset)
        {
            throw new NotSupportedException ();
        }

        public int Read (Request[] buffer, int offset, int size)
        {
            throw new NotSupportedException ();
        }

        /// <summary>
        /// Write a response to the client.
        /// </summary>
        public void Write (Response value)
        {
            stream.Write (Encoder.EncodeResponse (value));
        }

        public void Write (Response[] value)
        {
            throw new NotSupportedException ();
        }

        public ulong BytesRead {
            get { return stream.BytesRead; }
        }

        public ulong BytesWritten {
            get { return stream.BytesWritten; }
        }

        public void ClearStats ()
        {
            stream.ClearStats ();
        }

        /// <summary>
        /// Close the stream.
        /// </summary>
        public void Close ()
        {
            buffer = null;
            bufferedRequest = null;
            stream.Close ();
        }

        /// Returns quietly if there is a message in bufferedRequest
        /// Throws NoRequestException if not
        /// Throws MalformedRequestException if malformed data received
        /// Throws RequestBufferOverflowException if buffer full but complete request not received
        void Poll ()
        {
            if (bufferedRequest != null)
                return;

            // If there's no further data, we won't be able to deserialize a request
            if (!stream.DataAvailable)
                throw new NoRequestException ();

            // Read as much data as we can from the client into the buffer, up to the buffer size
            offset += stream.Read (buffer, offset);

            // Try decoding the request
            bufferedRequest = Encoder.DecodeRequest (buffer, 0, offset);

            // Valid request received, reset the buffer
            offset = 0;
        }
    }
}

