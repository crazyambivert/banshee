/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  BansheeDbusClient.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
// This example is a very basic D-Bus client implementation of the Banshee
// D-Bus API. This API *will* change very soon. It will also be possible
// to link against a Banshee assembly that will provide the implementation
// of the BansheeCore class (or clients can continue to implement the
// abstract methods like below). 
//
// There is currently no event/signal support, which means the client
// must poll the server in a loop or similar to get status updates, etc.

using System;
using DBus;
using Gtk;

[Interface("org.gnome.Banshee.Player")]
public abstract class BansheePlayer
{       
    public static BansheePlayer FindInstance()
    {
        Connection connection = Bus.GetSessionBus();
        Service service = Service.Get(connection, "org.gnome.Banshee");        
        return (BansheePlayer)service.GetObject(typeof(BansheePlayer), "/org/gnome/Banshee/Player");
    }

    [Method] public abstract void PresentWindow();
    [Method] public abstract void HideWindow();
    [Method] public abstract void ShowWindow();
    [Method] public abstract void TogglePlaying();
    [Method] public abstract void Play();
    [Method] public abstract void Pause();
    [Method] public abstract void Next();
    [Method] public abstract void Previous();
    [Method] public abstract string GetPlayingArtist();
    [Method] public abstract string GetPlayingAlbum();
    [Method] public abstract string GetPlayingTitle();
    [Method] public abstract string GetPlayingGenre();
    [Method] public abstract string GetPlayingUri();
    [Method] public abstract int GetPlayingPosition();
    [Method] public abstract int GetPlayingDuration();
    [Method] public abstract int GetPlayingStatus();
    [Method] public abstract void SetPlayingPosition(int position);
    [Method] public abstract void SkipForward();
    [Method] public abstract void SkipBackward();
    [Method] public abstract void SetVolume(int volume);
    [Method] public abstract void IncreaseVolume();
    [Method] public abstract void DecreaseVolume();
}

public class BansheeDbusClient : Window
{
    private static BansheePlayer banshee = null;

    public static void Main()
    {   
        try {
            banshee = BansheePlayer.FindInstance();
        } catch(Exception) {
            Console.Error.WriteLine("Could not locate Banshee on D-Bus. Perhaps it's not running?");
            Environment.Exit(1);
        }
        
        Application.Init();
        new BansheeDbusClient();
        Application.Run();
    }
    
    private Button previous_button;
    private Button playpause_button;
    private Button next_button;
    private Label status_label;
    private Label artist_label;
    private Label album_label;
    private Label title_label;
    
    private BansheeDbusClient() : base("Banshee D-Bus Client")
    {
        BorderWidth = 10;
    
        VBox box = new VBox();
        box.Spacing = 5;
        Add(box);
        
        HBox button_box = new HBox();
        previous_button = new Button("<< Previous");
        playpause_button = new Button("Play/Pause");
        next_button = new Button("Next >>");
        
        previous_button.Clicked += delegate(object o, EventArgs args) {
            banshee.Previous();
            QueryServer();
        };
        
        playpause_button.Clicked += delegate(object o, EventArgs args) {
            if(banshee.GetPlayingStatus() == -1) {
                return;
            }
            
            banshee.TogglePlaying();
            QueryServer();
        };
        
        next_button.Clicked += delegate(object o, EventArgs args) {
            banshee.Next();
            QueryServer();
        };
        
        button_box.PackStart(previous_button, false, false, 0);
        button_box.PackStart(playpause_button, true, true, 0);
        button_box.PackStart(next_button, false, false, 0);
        
        box.PackStart(button_box, false, false, 0);
        
        status_label = new Label("Connecting...");
        artist_label = new Label();
        album_label = new Label();
        title_label = new Label();
        
        box.PackStart(status_label, false, false, 0);
        box.PackStart(artist_label, false, false, 0);
        box.PackStart(album_label, false, false, 0);
        box.PackStart(title_label, false, false, 0);
        
        ShowAll();
        
        QueryServer();
        GLib.Timeout.Add(500, QueryServer);
    }
    
    protected override bool OnDeleteEvent(Gdk.Event evnt)
    {
        Application.Quit();
        return true;
    }
    
    private void SetStatus(string status)
    {
        TimeSpan position = new TimeSpan(banshee.GetPlayingPosition() * TimeSpan.TicksPerSecond);
        TimeSpan duration = new TimeSpan(banshee.GetPlayingDuration() * TimeSpan.TicksPerSecond);
        
        status_label.Markup = String.Format(
            "<b>Status:</b> {0} ({1:00}:{2:00} / {3:00}:{4:00})", status,
            position.Minutes, position.Seconds, 
            duration.Minutes, duration.Seconds);
    }
    
    private void SetField(string fieldName, string value, Label label)
    {
        if(value == null || value == String.Empty) {
            label.Hide();
            return;
        }
        
        label.Markup = String.Format("<b>{0}</b>: {1}", fieldName,
            GLib.Markup.EscapeText(value));
        label.Show();
    }
    
    private string last_uri = null;
    
    private bool QueryServer()
    {
        int status = -1;
        
        try {
            status = banshee.GetPlayingStatus();
        } catch(Exception) {
            Console.Error.WriteLine("Lost connection to Banshee Server");
            Application.Quit();
            return false;
        }
        
        switch(status) {
            case 0:
                SetStatus("Paused");
                break;
            case 1:
                SetStatus("Playing");
                break;
            case -1:
            default: 
                status_label.Markup = "<b>Status:</b> No song loaded";
                artist_label.Hide();
                album_label.Hide();
                title_label.Hide();
                return true;
        }
        
        string uri = banshee.GetPlayingUri();
        
        if(uri != last_uri) {
            last_uri = uri;
            
            Console.WriteLine("Song Changed: {0}", uri);
            
            SetField("Artist", banshee.GetPlayingArtist(), artist_label);
            SetField("Album", banshee.GetPlayingAlbum(), album_label);
            SetField("Title", banshee.GetPlayingTitle(), title_label);
        }
        
        return true;
    }
}
