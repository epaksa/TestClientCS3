using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClientCS.Common.DataStructure
{
    class RingBuffer
    {
        public byte[] _buffer = new byte[Program.READ_BUFFER_SIZE];
        public int _read_index = 0;
        public int _write_index = 0;

        public bool Push(byte[] data, int size)
        {
            if (false == CanPush(size))
            {
                return false;
            }

            for (int i = 0; i < size; ++i)
            {
                _buffer[_write_index++] = data[i];
                _write_index %= _buffer.Length;
            }

            return true;
        }

        public bool Pop(ArraySegment<byte> result_buffer, int size)
        {
            if (false == CanPop(size))
            {
                return false;
            }

            for (int i = 0; i < size; ++i)
            {
                result_buffer.Array[result_buffer.Offset + i] = _buffer[_read_index++];
                _read_index %= _buffer.Length;
            }

            return true;
        }

        public void Clear()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _read_index = 0;
            _write_index = 0;
        }

        public int PopAll(ArraySegment<byte> result_buffer)
        {
            int pop_count = 0;

            while (false == Empty())
            {
                Pop(new ArraySegment<byte>(result_buffer.Array, result_buffer.Offset + pop_count, result_buffer.Count - pop_count), 1);
                ++pop_count;
            }

            return pop_count;
        }

        public byte[] Data()
        {
            return _buffer;
        }

        public bool Empty()
        {
            return (_read_index == _write_index);
        }

        public int GetLength()
        {
            return _buffer.Length;
        }

        public bool SetWriteIndex(int index)
        {
            if (index >= Program.READ_BUFFER_SIZE)
            {
                return false;
            }

            _write_index += index;
            _write_index %= Program.READ_BUFFER_SIZE;

            return true;
        }

        public void Copy(ref RingBuffer buffer)
        {
            Array.Copy(buffer._buffer, _buffer, _buffer.Length);
            _read_index = buffer._read_index;
            _write_index = buffer._write_index;
        }

        private bool CanPush(int size)
        {
            return (size < _buffer.Length && (_write_index + size - _buffer.Length) < _read_index);
        }

        private bool CanPop(int size)
        {
            return (size < _buffer.Length && (_read_index + size - _buffer.Length) <= _write_index);
        }
    }
}
