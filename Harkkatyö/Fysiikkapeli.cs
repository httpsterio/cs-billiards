using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

public class Harkkatyö : PhysicsGame
{
    bool CANHIT = true;
    int HITCOUNTER = 0;

    public override void Begin()
    {
        Mouse.IsCursorVisible = true;


        // Alustetaan fysiikkaolioita. Näitä tarvitaan eri aliohjelmien välillä ja niitä välitetään parametrina toisilleen, muutoin nämä olisivat aliohjelmissaan.
        PhysicsObject valkoinenPallo = new PhysicsObject(16,16);
        PhysicsObject maila = new PhysicsObject(17, 328);
        PhysicsObject tasku = new PhysicsObject(80,80);

        // Kutsutaan tarvittavat aliohjelmat
        LuoKentta();
        LuoValkoinenPallo(valkoinenPallo);
        LuoMaila(maila, valkoinenPallo);
        LuoOhjaimet(maila, valkoinenPallo);

        Tormaykset(valkoinenPallo);
        Sounds.PlayMusic();
    }

    public void Tormaykset(PhysicsObject tormaaja)
    {

        void seinatormays(PhysicsObject pallo, PhysicsObject kohde)
        {
            if (kohde.Tag.ToString() == "laita" || kohde.Tag.ToString() == "kulma")
            {
                Sounds.PlayWall();
            }
        }
        AddCollisionHandler(tormaaja, seinatormays);
    }
    public void LuoOhjaimet(PhysicsObject maila, PhysicsObject valkoinenPallo)
    {

        Mouse.ListenMovement(0.1, SiirraMaila, "liikuta mailaa", maila, valkoinenPallo);
        double voima = 10000;
        Mouse.Listen(MouseButton.Left, ButtonState.Down, delegate () { MessageDisplay.Add("lol"); }, null);
        Mouse.Listen(MouseButton.Left, ButtonState.Down, delegate() { LyoPalloa(valkoinenPallo, maila, ref voima); } , "Lyö palloa");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.R, ButtonState.Pressed, delegate()
        {
            Reset(maila, valkoinenPallo);
        }, "Resetoi peli");

        void AsetaVoima(int value)
        {
            MessageDisplay.Add("Lyönnin voimakkuus on " + value);
        }

    }

    public void LuoValkoinenPallo(PhysicsObject valkoinenPallo)
    {
        valkoinenPallo.Shape = Shape.Circle;
        valkoinenPallo.X = 0;
        valkoinenPallo.Y = 0;
        Add(valkoinenPallo);
    }

    public void LuoKentta()
    {
        // Asettaa ikkunan koon, laittaa pelille laidat ja zoomaa pelin näkyviin elementteihin.
        SetWindowSize(1280, 1024);
        Level.CreateBorders();
        Camera.ZoomToLevel();

        // Lataa kuvasta kentän laidat ja luo gameobjektin.
        GameObject kentta = new GameObject(784,448);
        kentta.Image = LoadImage("poyta");
        kentta.Shape = Shape.Rectangle;
        kentta.Position = new Vector(0,0);
        Add(kentta,-1);

        // Lataa kuvasta taskut ja luo siitä gameobjektin.
        GameObject taskut = new GameObject(784, 448);
        taskut.Image = LoadImage("taskut");
        taskut.Shape = Shape.Rectangle;
        taskut.Position = new Vector(0, 0);
        Add(taskut, -2);

        // Asettaa pelikentälle taustaobjektin.
        GameObject kangas = new GameObject(784, 448);
        kangas.Color = Color.Green;
        kangas.Shape = Shape.Rectangle;
        kangas.Position = new Vector(0, 0);
        Add(kangas, -3);

        // Alempana on kolme listaa, jotka sisältävät koordinaatteja, kokoja ja mahdollisesti kallistuskulmia
        // joiden avulla generoidaan fysiikkaobjekteja (laidat, taskut ja taskujen supistajat). Alkuperäisessä 
        // ideassa Kenttä koostuu läpinäkyvistä kuvista ja kuvien tasoista voitaisiin generoida kaikki tarvittavat
        // ilman erillistä listaa, mutta Jypeli ei tue ns. onttoja kuvia, eli kentän laidat eivät voineet toimia
        // sisälaitoina, joten tässä on purkkaratkaisu. 

        // Lista, jossa on laitablokkien koot (korkeus, leveys) ja sijainti vektoreina. Näitä käytetään törmäyksissä ja elementit on listassa jotta niitä voidaan kutsua loopilla.
        var LaitaLista = new List<(double, double, Vector)>
        {
            (64, 312, new Vector(-376, 0)),
            (64, 312, new Vector(376, 0)),
            (296, 64, new Vector(-176, 209)),
            (296, 64, new Vector(-176, -209)),
            (296, 64, new Vector(176, 209)),
            (296, 64, new Vector(176, -209))
        };

        // Iteroi listan läpi ja välittää listan itemeiden arvot parametrina funktiolle joka luo laidat
        foreach (var item in LaitaLista)
        {
            LuoLaita(item.Item1, item.Item2, item.Item3);
        }

        // Lista Nurkista joissa on törmäyksentunnistus ja pallot interaktaavat näiden kanssa.
        var TaskuLista = new List<(Vector, double)>
        {
            (new Vector(-376, 200), 45),
            (new Vector(376, 200), -45),
            (new Vector(-376, -200), -45),
            (new Vector(376, -200), 45),
            (new Vector(0, 228), 0),
            (new Vector(0, -228), 0)
        };

        // Iteroi listan läpi ja välittää listan arvot parametrina funktiolle joka luo taskut törmäyksiä varten 
        foreach (var item in TaskuLista)
        {
            LuoTasku(item.Item1, item.Item2);
        }

        var KulmaLista = new List<(Vector, double)>
        {
        (new Vector(-364, 162), 60),
        (new Vector(364, 162), -60),
        (new Vector(-364, -162), -60),
        (new Vector(364, -162), 60),
        (new Vector(-330, 196), 30),
        (new Vector(330, 196), -30),
        (new Vector(-330, -196), -30),
        (new Vector(330, -196), 30),
        (new Vector(-24, 197), -24),
        (new Vector(-24, -197), 24),
        (new Vector(24, 197), 24),
        (new Vector(24, -197), -24)
        };

        // Iteroi listan läpi ja välittää listan arvot parametrina funktiolle joka luo taskut törmäyksiä varten 
        foreach (var item in KulmaLista)
        {
            LuoLaitaKulma(item.Item1, item.Item2);
        }
    }

    public void LuoLaitaKulma(Vector sijainti, double kallistus)
    {
        PhysicsObject kulma = new PhysicsObject(10, 40);
        kulma.Shape = Shape.Rectangle;
        kulma.Position = sijainti;
        kulma.MakeStatic();
        kulma.Color = Color.Transparent;
        kulma.Tag = "kulma";
        kulma.Angle = Angle.FromDegrees(kallistus);
        Add(kulma, 3);
        #if DEBUG
            kulma.Color = Color.Red;
        #endif

    }

    // Funktio joka luo taskun. Taskulla on törmäyksentunnistus
    public void LuoTasku(Vector sijainti, double kallistus)
    {
        PhysicsObject taskuCollision = new PhysicsObject(128, 64);
        taskuCollision.Color = Color.Transparent;
        taskuCollision.Position = sijainti;
        taskuCollision.MakeStatic();
        taskuCollision.Shape = Shape.Rectangle;
        taskuCollision.Angle = Angle.FromDegrees(kallistus);
        taskuCollision.Tag = "taskucollision";
        Add(taskuCollision);
        #if DEBUG
            taskuCollision.Color = Color.Pink;
        #endif

    }


    // Funktio joka luo näkymättömät sivulaidat törmäystä varten
    public void LuoLaita(double leveys, double korkeus, Vector sijainti) {
        PhysicsObject laita = new PhysicsObject(leveys, korkeus);
        laita.Shape = Shape.Rectangle;
        laita.Position = sijainti;
        laita.MakeStatic();
        laita.Color = Color.Transparent;
        laita.Tag = "laita";
        Add(laita);
        #if DEBUG
            laita.Color = Color.Blue;
        #endif

    }

    public void LuoMaila(PhysicsObject maila, PhysicsObject valkoinenPallo)
    {
        Vector paikkaruudulla = Mouse.PositionOnScreen;
        maila.Color = Color.Transparent;
        maila.Shape = Shape.Rectangle;
        maila.Image = LoadImage("maila");
        maila.X = paikkaruudulla.X;
        maila.Y = paikkaruudulla.Y;
        maila.Angle = Angle.FromDegrees(0);
        maila.IgnoresCollisionResponse = true;
        Add(maila);

        Timer mailanAjastin = new Timer();
        mailanAjastin.Interval = 0.016;
        mailanAjastin.Timeout += delegate()
        {
            if (Math.Abs(valkoinenPallo.Velocity.X) > 0.9 || Math.Abs(valkoinenPallo.Velocity.Y) > 0.9)
            {
                maila.Size = new Vector(1,1);
                CANHIT = false;
            }
            else
            {
                maila.Size = new Vector(17,328);
                CANHIT = true;

            }

        };
        mailanAjastin.Start();
    }

    public void SiirraMaila(PhysicsObject maila, PhysicsObject pallo)
    {
        maila.Position = Mouse.PositionOnScreen;
        double posX = maila.Position.X - pallo.Position.X;
        double posY = maila.Position.Y - pallo.Position.Y;
        maila.Angle = Angle.FromRadians(Math.Atan2(posY, posX) + Math.PI/2);
    }

    public void LyoPalloa(PhysicsObject pallo, PhysicsObject maila, ref double voima)
    {
        Vector suunta = new Vector(pallo.X - maila.X, pallo.Y - maila.Y);
        if (CANHIT == true) { 
            pallo.Push(suunta.Normalize() * voima);
            HITCOUNTER++;
        }
        else
        {
            Sounds.PlayError();
        }
        pallo.LinearDamping = 0.985;
    }

    public void Reset(PhysicsObject maila, PhysicsObject valkoinenPallo)
    {
        ClearAll();
        LuoKentta();
        LuoValkoinenPallo(valkoinenPallo);
        LuoMaila(maila, valkoinenPallo);
        LuoOhjaimet(maila, valkoinenPallo);

    }

    public void Pallot()
    {

    }

    public static class Sounds
    {
        static SoundEffect error = LoadSoundEffect("false");
        static SoundEffect seina = LoadSoundEffect("wall");
        static SoundEffect intro = LoadSoundEffect("intro");
        static SoundEffect bg = LoadSoundEffect("bg");
        
        public static void PlayError()
        {
            error.Play(0.2, 0, 0);
        }

        public static void PlayWall()
        {
            seina.Play(0.1, 0, 0);
        }

        public static void PlayMusic()
        {
            intro.Play(0.3, 0, 0);
            Timer.SingleShot(6.80, delegate { bg.Play(0.3,0,0); });
            Timer.CreateAndStart(144.0, delegate { bg.Play(0.3, 0, 0); });
        }
        
    }
}
