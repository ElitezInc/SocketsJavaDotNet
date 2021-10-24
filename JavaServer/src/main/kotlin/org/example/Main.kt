package org.example

import java.io.*
import java.lang.Thread.sleep
import java.net.ServerSocket
import java.net.Socket
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.charset.StandardCharsets
import java.nio.file.Files

internal object Main
{
    val BUFFER_SIZE = 1024

    @JvmStatic
    fun main(args: Array<String>)
    {
        socketServer()
    }

    fun socketServer()
    {
        val server = ServerSocket(9092)

        println("Waiting for the client connection")

        val socket: Socket = server.accept()

        println("Client connected")

        val output = BufferedOutputStream(socket.getOutputStream())
        val input =  BufferedInputStream(socket.getInputStream())

        val listenThread = Thread { listenFunctionality(input) }
        val sendThread = Thread { sendFunctionality(output) }

        listenThread.start()
        sendThread.start()

        while (listenThread.isAlive && sendThread.isAlive)
        {
            sleep(50)
        }

        println("Shutting down Socket server")
        listenThread.stop()
        sendThread.stop()
        output.close()
        input.close()
        server.close()
    }

    fun listenFunctionality(input: InputStream)
    {
        while (true)
        {
            try
            {
                if (isReceiveText(input))
                {
                    val size = receiveSize(input)
                    println("Received text: ${String(receiveBytes(input, size), StandardCharsets.UTF_8)}")
                }
                else
                {
                    val size = receiveSize(input)
                    receiveFile(input, File("Received.jpg"))
                    println("Received file saved")
                }
            }
            catch (err : Exception)
            {
                err.printStackTrace()
                break;
            }
        }
    }

    fun sendFunctionality(output : OutputStream)
    {
        while (true)
        {
            try
            {
                sendFile(output, false, File("Capture.jpg"))
                println("Sent binary file")
                sleep((Math.random() * 10000).toLong())

                sendFile(output, true, File("data.json"))
                println("Sent text file")
                sleep((Math.random() * 10000).toLong())
            }
            catch (err : Exception)
            {
                err.printStackTrace()
                break;
            }
        }
    }

    fun sendBytes(output: OutputStream, isText : Boolean, bytes : ByteArray)
    {
        if (isText)
            output.write("s".toByteArray(StandardCharsets.UTF_8))
        else
            output.write("b".toByteArray(StandardCharsets.UTF_8))
        output.flush()

        output.write(ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(bytes.size).array())
        output.flush()
        output.write(bytes)
        output.flush()
    }

    fun sendText(output : OutputStream, text : String)
    {
        output.write("s".toByteArray(StandardCharsets.UTF_8))
        output.flush()
        output.write(ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(text.length).array())
        output.flush()
        output.write(text.toByteArray(StandardCharsets.UTF_8))
        output.flush()
    }

    fun sendFile(output : OutputStream, isText : Boolean, file : File)
    {
        if (isText)
            output.write("s".toByteArray(StandardCharsets.UTF_8))
        else
            output.write("b".toByteArray(StandardCharsets.UTF_8))
        output.flush()

        output.write(ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(Files.size(file.toPath()).toInt()).array())
        output.flush()

        val buffer = ByteArray(BUFFER_SIZE)
        val input = BufferedInputStream(FileInputStream(file))
        var readLength : Int

        while (input.read(buffer).also { readLength = it } > 0)
        {
            output.write(buffer, 0, readLength)
            output.flush()
        }

        input.close()
    }

    fun isReceiveText(input : InputStream) : Boolean
    {
        val typeByte = ByteArray(1)
        input.read(typeByte)
        val dataType = String(typeByte, StandardCharsets.UTF_8)

        return dataType == "s"
    }

    fun receiveSize(input : InputStream) : Int
    {
        val lengthBytes = ByteArray(4)
        input.read(lengthBytes)
        return ByteBuffer.wrap(lengthBytes).int
    }

    fun receiveBytes(input : InputStream, size : Int) : ByteArray
    {
        val dataBytes = ByteArray(size)
        var remaining = size
        var offset = 0

        while (remaining > 0)
        {
            val count: Int = input.read(dataBytes, offset, remaining)
            if (-1 == count) break

            remaining -= count
            offset += count
        }

        return dataBytes
    }

    fun receiveFile(input : InputStream, file : File)
    {
        val buffer = ByteArray(BUFFER_SIZE)
        val bos = BufferedOutputStream(FileOutputStream(file, false))
        var readLength: Int

        while (input.read(buffer).also { readLength = it } > 0)
        {
            bos.write(buffer, 0, readLength)
            bos.flush()
            if (readLength < buffer.size) break
        }

        bos.close()
    }
}