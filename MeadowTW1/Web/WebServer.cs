﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace NewCode.Web {
    public class WebServer {

        private IPAddress _ip = null;
        private int _port = -1;
        private bool _runServer = true;
        private static HttpListener listener;
        private static int pageViews = 0;
        private static int requestCount = 0;
        private static bool ready = false;
        private static readonly string pass = "pass";
        private static string message = "";


        /// <summary>
        /// Delegate for the CommandReceived event.
        /// </summary>
        public delegate void CommandReceivedHandler(object source, WebCommandEventArgs e);

        /// <summary>
        /// CommandReceived event is triggered when a valid command (plus parameters) is received.
        /// Valid commands are defined in the AllowedCommands property.
        /// </summary>
        public event CommandReceivedHandler CommandReceived;

        public string Url {
            get {
                if (_ip != null && _port != -1) {
                    return $"http://{_ip}:{_port}/";
                }
                else {
                    return $"http://127.0.0.1:{_port}/";
                }
            }
        }

        public WebServer(IPAddress ip, int port) {
            _ip = ip;
            _port = port;
        }


        public async void Start() {
            if (listener == null) {
                listener = new HttpListener();
                listener.Prefixes.Add(Url);

            }

            listener.Start();

            Console.WriteLine($"The url of the webserver is {Url}");

            // Handle requests
            while (_runServer) {
                await HandleIncomingConnections();
            }

            //await HandleIncomingConnections();

            // Close the listener
            listener.Close();
        }

        public async void Stop() {
            _runServer = false;
        }

        private async Task HandleIncomingConnections() {

            await Task.Run(async () => {
                // While a user hasn't visited the `shutdown` url, keep on handling requests
                while (_runServer) {

                    // Will wait here until we hear from a connection
                    HttpListenerContext ctx = await listener.GetContextAsync();

                    // Peel out the requests and response objects
                    HttpListenerRequest req = ctx.Request;
                    HttpListenerResponse resp = ctx.Response;

                    // Print out some info about the request
                    Console.WriteLine("Request #: {0}", ++requestCount);
                    Console.WriteLine(req.Url);
                    Console.WriteLine(req.HttpMethod);
                    Console.WriteLine(req.UserHostName);
                    Console.WriteLine(req.UserAgent);
                    Console.WriteLine();


                    // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                    if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/shutdown") {
                        Console.WriteLine("Shutdown requested");
                        _runServer = false;
                    }

                    if (req.Url.AbsolutePath == "/setparams") {

                        //Get parameters
                        string url = req.RawUrl;
                        if (!string.IsNullOrWhiteSpace(url)) {

                            //Get text to the right from the interrogation mark
                            string[] urlParts = url.Split('?');
                            if (urlParts?.Length >= 1) {

                                //The parametes are in the array first position
                                string[] parameters = urlParts[1].Split('&');
                                if (parameters?.Length >= 2) {

                                    // Param 5 => to pass
                                    string[] pass_parts = parameters[5].Split('=');
                                    string pass_temp = pass_parts[1];

                                    if (string.Equals(pass, pass_temp)) {
                                        //para varias rondas: Coger el formulario y permitir muchos valores en un solo input y
                                        //separarlo por un separador.
                                        string[] temp_max_parts = parameters[0].Split('=');
                                        string[] all_temperatures_max = temp_max_parts[1].Split(';');
                                        Data.temp_max = new string[all_temperatures_max.Length];
                                        all_temperatures_max.CopyTo(Data.temp_max, 0);
                                        //comprobar cada valor de all_temperatures_max[] en 0,1,2,...
                                        // Param 0 => Temp max
                                        //string[] temp_max_parts = parameters[0].Split('=');
                                        //crea un array de strings "temp_max_parts" y coge el parametro 0 desde la url.
                                        //Coge el valor desde el = y lo utiliza como separador. Se guarda el valor en el segundo elemento
                                        //de la cadena ya que el primero es un espacio 
                                        //Data.temp_max = new string[] { temp_max_parts[1] };
                                        //crea el array de temp maxima con el valor de temp_max_parts en la posicion 1, es decir, 
                                        //el valor recibido en el formulario en el parametro 0.

                                        // Param 1 => Temp min
                                        string[] temp_min_parts = parameters[1].Split('=');
                                        string[] all_temperatures_min = temp_min_parts[1].Split(';');
                                        Data.temp_min = new string[all_temperatures_min.Length];
                                        all_temperatures_min.CopyTo(Data.temp_min, 0);
                                        //Data.temp_min = new string[] { temp_min_parts[1] };

                                        // Param 2 => to display_refresh
                                        string[] display_refresh_parts = parameters[2].Split('=');


                                        // Param 3 => to refresh
                                        string[] refresh_parts = parameters[3].Split('=');


                                        // Param 4 => to round_time
                                        string[] round_time_parts = parameters[4].Split('=');
                                        string[] all_rounds = round_time_parts[1].Split(';');
                                        Data.round_time = new string[all_rounds.Length];
                                        all_rounds.CopyTo(Data.round_time, 0);

                                        if (timeRefreshCheck(display_refresh_parts[1]) && timeRefreshCheck(refresh_parts[1]))
                                        {
                                            Data.refresh = Int16.Parse(refresh_parts[1]);
                                            Data.display_refresh = Int16.Parse(display_refresh_parts[1]);

                                            if (timeCheck(Data.round_time))
                                            {
                                                if (!tempConsistantCheck(Data.temp_max, Data.temp_min))
                                                {
                                                    message = "El rango de temperatura maximo es entre 30 y 12 grados C. Ademas, " +
                                                    "el valor debe ser numerico y tener coherencia. Revisa los parametros de temperatura";
                                                }

                                                else
                                                {
                                                    if (Data.round_time.Length==Data.temp_max.Length) { 
                                                    message = "Los parametros se han cambiado satisfactoriamente. Todo preparado.";
                                                    ready = true;
                                                    }
                                                    else
                                                    {
                                                        message = "El tiempo y las temperaturas tienen que tener el mismo numero de rondas";
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                message = "El tiempo no es correcto";
                                            }
                                        }
                                        else
                                        {
                                            message = "Los tiempos de refresco son incorrectos";
                                        }
                                    }
                                    else
                                    {
                                        message = "La contrasenia es incorrecta.";
                                    }
                                }
                            }
                        }

                    }
                    if (req.Url.AbsolutePath == "/start") {                        
                        // Start the round
                        Thread ronda = new Thread(MeadowApp.StartRound);
                        ronda.Start();

                        // Wait for the round to finish
                        while (Data.is_working) {
                            Thread.Sleep(1000);
                        }
                        ready = false;
                        int value = MeadowApp.total_time_in_range;
                        message = "Se ha terminado la ronda con " + value + "s en el rango indicado.";
                        Console.WriteLine(value);
                    }

                    // Write the response info
                    string disableSubmit = !_runServer ? "disabled" : "";
                    byte[] data = Encoding.UTF8.GetBytes(string.Format(writeHTML(message), pageViews, disableSubmit));
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    // Write out to the response stream (asynchronously), then close it


                    //opcion quitar esto para que sea infinito
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
            });
        }


        public static string mostarDatos(string[] data) {
            string datos = string.Empty;
            if (data != null) {
                for (int i = 0; i < data.Length; i++) {
                    datos = datos + data[i];
                }

                return datos;
            }
            else {
                return "";
            }
        }

        public static bool timeRefreshCheck(string data)
        {
            bool result;
            float aux = 0;
            if (data != null)
            {
                result = float.TryParse(data, out aux);
                if (result == true)
                {
                    if (int.Parse(data) > 0)
                    {
                        return true;
                    }
                    return false;
                }
                return false;

            }
            return false;
        }

        public static bool timeCheck(string[] data)
        {
            bool result;
            short aux = 0;
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    result = Int16.TryParse(data[i], out aux);
                    if (result == true)
                    {
                        if (int.Parse(data[i].ToString()) < 0)
                        {
                            return true;
                        }

                    }
                    else
                    {
                        return false;
                    }

                }
                return true;
            }
            return false;
        }

        public static bool tempCheck(string[] data) {
            bool result;
            float aux = 0;
            if (data != null) {
                for (int i = 0; i < data.Length; i++)
                {
                    result = float.TryParse(data[i], out aux);
                    if (result == true)
                    {
                        if (float.Parse(data[i].ToString()) < 12 || float.Parse(data[i].ToString()) > 30)
                        {
                            return false;
                        }
                        //return true;//eliminar
                    }
                    else {
                        return false;
                    }
                    
                }
                return true;
            }
            return false;
        }

        public static bool tempConsistantCheck(string[] dataMax, string[] dataMin)
        {
            if (dataMax != null && dataMin != null)
            {
                if (tempCheck(dataMax) && tempCheck(dataMin))
                {
                    if (dataMax.Length == dataMin.Length) { 
                        for (int i = 0; i < dataMax.Length; i++)
                        {
                            if (float.Parse(dataMax[i].ToString()) < float.Parse(dataMin[i].ToString()))
                            {
                                return false;
                            }

                        }
                        return true;
                    }
                    return false;
                }
                return false;
            }
            return false;
        }

        public static string writeHTML(string message) {
            // If we are already ready, disable all the inputs
            string disabled = "";
            if (ready) {
                disabled = "disabled";
            }

            // Only show save and cooler mode in configuration mode and start round when we are ready
            //string inicio = "<button type=\"button\" onclick='inicio()'>Inicio</button>";
            string save = "<button type=\"button\" onclick='save()'>Guardar</button>";
            string temp = "<a href='#' class='btn btn-primary tm-btn-search' onclick='temp()'>Consultar Temperatura</a>";
            string graph = "";
            if (ready) {
                save = "";
            }
            string start = "";
            if (ready) {
                start = "<button type=\"button\" onclick='start()'>Comenzar Ronda</button>";
            }
            if (Data.is_working) {
                start = "";
            }
            /*if (Data.csv_counter != 0) {
                graph = "<canvas id='myChart' width='0' height='0'></canvas>";
                message = "El tiempo que se ha mantenido en el rango de temperatura es de " + Data.time_in_range_temp.ToString() + " s.";
            }*/



            //Write the HTML page
            string html = "<!DOCTYPE html>" +
            "<html>" +
            "<head>" +
                            "<meta charset='utf - 8'>" +
                            "<meta http - equiv = 'X-UA-Compatible' content = 'IE=edge'>" +
                            "<meta name = 'viewport' content = 'width=device-width, initial-scale=1' > " +
                            "<title>Netduino Plus 2 Controller</title>" +
                            "<link rel='stylesheet' href='https://fonts.googleapis.com/css?family=Open+Sans:300,400,600,700'>" +
                            "<link rel = 'stylesheet' href = 'http://127.0.0.1:8887/css/bootstrap.min.css'>" +
                            "<link rel = 'stylesheet' href = 'http://127.0.0.1:8887/css/tooplate-style.css' >" +
                            "<script src='https://cdnjs.cloudflare.com/ajax/libs/Chart.js/3.8.0/chart.js'> </script>" +


            "</head>" +

            "<body>" +
                            "<script> function save(){{" +
                            "console.log(\"SAVE!!\");" +
                            "var tempMax = document.forms['params']['tempMax'].value;" +
                            "var tempMin = document.forms['params']['tempMin'].value;" +
                            "var displayRefresh = document.forms['params']['displayRefresh'].value;" +
                            "var refresh = document.forms['params']['refresh'].value;" +
                            "var time = document.forms['params']['time'].value;" +
                            "var pass = document.forms['params']['pass'].value;" +
                            "location.href = 'setparams?tempMax=' + tempMax + '&tempMin=' + tempMin + '&displayRefresh=' + displayRefresh + '&refresh=' + refresh + '&time=' + time + '&pass=' + pass;" +
                            "}} " +
                            "function start(){{location.href = 'start'}}" +
                            "function inicio(){{location.href = '#'}}" +
                            "</script>" +

                            "<div class='tm-main-content' id='top'>" +
                            "<div class='tm-top-bar-bg'></div>" +
                            "<div class='container'>" +
                            "<div class='row'>" +
                            "<nav class='navbar navbar-expand-lg narbar-light'>" +
                            "<a class='navbar-brand mr-auto' href='#'>" +
                            "<img id='logo' class='logo' src='http://127.0.0.1:8887/img/6.webp' alt='Site logo' width='700' height='300'>" +
                            "</a>" +
                            "</nav>" +
                            "</div>" +
                            "</div>" +
                            "</div>" +
                            "<div class='tm-section tm-bg-img' id='tm-section-1'>" +
                            "<div class='tm-bg-white ie-container-width-fix-2'>" +
                            "<div class='container ie-h-align-center-fix'>" +
                            "<div class='row'>" +
                            "<div class='col-xs-12 ml-auto mr-auto ie-container-width-fix'>" +
                            "<div class='form-group tm-form-element tm-form-element-50'>" +
                            //inicio +
                            "</div>" + "<ln>"+
                            "<form name='params' method = 'get' class='tm-search-form tm-section-pad-2'>" +
                            "<div class='form-row tm-search-form-row'>" +
                            "<div class='form-group tm-form-element tm-form-element-100'>" +
                            "<p>Temperatura Max <b>(&deg;C)</b> <input name='tempMax' type='text' class='form-control' value='" + mostarDatos(Data.temp_max) + "' " + disabled + "></input></p>" +
                            "</div>" +
                            "<div class='form-group tm-form-element tm-form-element-50'>" +
                            "<p>Temperatura Min <b>(&deg;C)</b> <input name='tempMin' type='text' class='form-control' value='" + mostarDatos(Data.temp_min) + "' " + disabled + "></input></p>" +
                            "</div>" +
                            "<div class='form-group tm-form-element tm-form-element-50'>" +
                            "<p>Duraci&oacute;n Ronda <b>(s)</b> <input name='time' type='text' class='form-control' value='" + mostarDatos(Data.round_time) + "' " + disabled + "></input></p>" +
                            "</div>" +
                            "</div>" +
                            "<div class='form-row tm-search-form-row'>" +
                            "<div class='form-group tm-form-element tm-form-element-100'>" +
                            "<p>Cadencia Refresco <b>(ms)</b> <input name='displayRefresh' type='number' class='form-control' value='" + Data.display_refresh + "' " + disabled + "></input></p>" +
                            "</div>" +
                            "<div class='form-group tm-form-element tm-form-element-50'>" +
                            "<p>Cadencia Interna <b>(ms)</b> <input name='refresh' type='number' class='form-control' value='" + Data.refresh + "' " + disabled + "></input></p>" +
                            "</div>" +
                            "<div class='form-group tm-form-element tm-form-element-50'>" +
                            "<p>Contrase&ntilde;a <input name='pass' type='password' class='form-control'> </input></p>" +
                            "</div>" +

                            "</form>" +
                            "<div class='form-group tm-form-element tm-form-element-50'>" +
                            save + start +
                            "</div>" +
                            "<div class='form-group tm-form-element tm-form-element-50'>" +
                            temp +
                            "</div>" +
                            "</div>" +
                            "<p style='text-align:center;font-weight:bold;'>" + message + "</p>" +
                            "</div>" +
                            "</div>" +
                            "</div>" +
                            "</div>" +
                            "</div>" +

                            "<div class='container ie-h-align-center-fix'>" +
                            graph +
                            "</div>" +
            "</body>" +
            "</html>";
            return html;
        }

    }
}
