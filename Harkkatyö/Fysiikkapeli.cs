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
    private SFX Sfx;

    public override void Begin()
    {
        Sfx = new SFX();
        Mouse.IsCursorVisible = true;


        // Alustetaan fysiikkaolioita. Näitä tarvitaan eri aliohjelmien välillä ja niitä välitetään parametrina toisilleen, muutoin nämä olisivat aliohjelmissaan.
        PhysicsObject valkoinenPallo = new PhysicsObject(16, 16);
        PhysicsObject maila = new PhysicsObject(17, 328);

        // Kutsutaan tarvittavat aliohjelmat alkuun. Yritin tässä kutsua Init()-metodia, mutta pelissä esiintyy bugeja kontrollien kanssa tällöin.
        LuoKentta();
        LuoValkoinenPallo(valkoinenPallo);
        LuoMaila(maila, valkoinenPallo);
        LuoOhjaimet(maila, valkoinenPallo);
        LuoPallot();
        Tormaykset(valkoinenPallo);
        Sfx.PlayMusic();

    }

    // Metodi eri törmäyksenkäsittelijöille
    public void Tormaykset(PhysicsObject tormaaja)
    {
        void seinatormays(PhysicsObject pallo, PhysicsObject kohde)
        {
            if (kohde.Tag.ToString() == "laita" || kohde.Tag.ToString() == "kulma")
            {
                Sfx.PlayWall();
            }
        }
        AddCollisionHandler(tormaaja, seinatormays);
    }

    public void LuoPallot()
    {
        double ballSize = 16;
        double r = ballSize / 2;
        double pyth = Math.Sqrt(Math.Pow(2 * r,2)-Math.Pow(r,2));
        var palloLista = new List<(double, double, String)>
        {
            (0, 0, "valkoinen"),

            (-pyth, (r), "1"),
            (-pyth, -(r), "2"),

            (-(2 * pyth), (2*r), "4"),
            (-(2*pyth), 0, "5"),
            (-(2*pyth), -(2*r), "6"),
        };

        foreach (var item in palloLista)
        {
            LuoPallo(item.Item1, item.Item2, ballSize, item.Item3);

        }

        void LuoPallo(double x, double y, double koko, String nimi)
        {
            PhysicsObject pallo = new PhysicsObject(koko, koko);
            pallo.X = x;
            pallo.Y = y;
            pallo.Tag = nimi;
            pallo.Shape = Shape.Circle;
            pallo.Color = Color.Transparent;
            pallo.Image = LoadImage(nimi);
            pallo.LinearDamping = 0.987;
            pallo.Mass = 0.05;

            Add(pallo, 1);
        }

    }
    public void LuoOhjaimet(PhysicsObject maila, PhysicsObject valkoinenPallo)
    {

        Mouse.ListenMovement(0.1, SiirraMaila, "liikuta mailaa", maila, valkoinenPallo);
        double voima = 3000;
        Mouse.Listen(MouseButton.Left, ButtonState.Down, delegate () {

        }, null);
        Mouse.Listen(MouseButton.Left, ButtonState.Pressed, delegate () { LyoPalloa(valkoinenPallo, maila, ref voima); MessageDisplay.Add(voima.ToString()); }, "Lyö palloa");
        Keyboard.Listen(Key.D1, ButtonState.Pressed, delegate () { voima = 100; }, "Aseta lyönnin voimakkuus"); // 1
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.R, ButtonState.Pressed, delegate ()
        {
            Reset(maila, valkoinenPallo);
        }, "Resetoi peli");

    }

    public void LuoValkoinenPallo(PhysicsObject valkoinenPallo)
    {
        valkoinenPallo.Shape = Shape.Circle;
        valkoinenPallo.X = 200;
        valkoinenPallo.Y = 0;
        valkoinenPallo.Mass = 0.1;
        Add(valkoinenPallo);
    }

    public void LuoKentta()
    {
        // Asettaa ikkunan koon, laittaa pelille laidat ja zoomaa pelin näkyviin elementteihin.
        SetWindowSize(1280, 1024);
        Level.CreateBorders();
        Camera.ZoomToLevel();

        // Lataa kuvasta kentän laidat ja luo gameobjektin.
        GameObject kentta = new GameObject(784, 448)
        {
            Image = LoadImage("poyta"),
            Shape = Shape.Rectangle,
            Position = new Vector(0, 0)
        };
        Add(kentta, -1);

        // Lataa kuvasta taskut ja luo siitä gameobjektin.
        GameObject taskut = new GameObject(784, 448)
        {
            Image = LoadImage("taskut"),
            Shape = Shape.Rectangle,
            Position = new Vector(0, 0)
        };
        Add(taskut, -2);

        // Asettaa pelikentälle taustaobjektin.
        GameObject kangas = new GameObject(784, 448)
        {
            Color = Color.Green,
            Shape = Shape.Rectangle,
            Position = new Vector(0, 0)
        };
        Add(kangas, -3);

        // Alempana on kolme listaa, jotka sisältävät koordinaatteja, kokoja ja mahdollisesti kallistuskulmia
        // joiden avulla generoidaan fysiikkaobjekteja (laidat, taskut ja taskujen supistajat). Alkuperäisessä 
        // ideassa Kenttä koostuu läpinäkyvistä kuvista ja kuvien tasoista voitaisiin generoida kaikki tarvittavat
        // ilman erillistä listaa, mutta Jypeli ei tue ns. onttoja kuvia, eli kentän laidat eivät voineet toimia
        // sisälaitoina, joten tässä on purkkaratkaisu. 

        // Lista, jossa on laitablokkien koot (korkeus, leveys) ja sijainti vektoreina. Näitä käytetään törmäyksissä ja elementit on listassa jotta niitä voidaan kutsua loopilla.
        var laitaLista = new List<(double, double, Vector)>
        {
            (64, 312, new Vector(-376, 0)),
            (64, 312, new Vector(376, 0)),
            (296, 64, new Vector(-176, 209)),
            (296, 64, new Vector(-176, -209)),
            (296, 64, new Vector(176, 209)),
            (296, 64, new Vector(176, -209))
        };

        // Iteroi listan läpi ja välittää listan itemeiden arvot parametrina funktiolle joka luo laidat
        foreach (var item in laitaLista)
        {
            LuoLaita(item.Item1, item.Item2, item.Item3);
        }

        // Lista Nurkista joissa on törmäyksentunnistus ja pallot interaktaavat näiden kanssa.
        var taskuLista = new List<(Vector, double)>
        {
            (new Vector(-376, 200), 45),
            (new Vector(376, 200), -45),
            (new Vector(-376, -200), -45),
            (new Vector(376, -200), 45),
            (new Vector(0, 228), 0),
            (new Vector(0, -228), 0)
        };

        // Iteroi listan läpi ja välittää listan arvot parametrina funktiolle joka luo taskut törmäyksiä varten 
        foreach (var item in taskuLista)
        {
            LuoTasku(item.Item1, item.Item2);
        }

        var kulmaLista = new List<(Vector, double)>
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
        foreach (var item in kulmaLista)
        {
            LuoLaitaKulma(item.Item1, item.Item2);
        }
    }

    public void LuoLaitaKulma(Vector sijainti, double kallistus)
    {
        PhysicsObject kulma = new PhysicsObject(10, 40)
        {
            Shape = Shape.Rectangle,
            Position = sijainti,
            Color = Color.Transparent,
            Tag = "kulma",
            Angle = Angle.FromDegrees(kallistus)
        };
        kulma.MakeStatic();
        Add(kulma, 3);
#if DEBUG
        kulma.Color = Color.Red;
#endif

    }

    // Funktio joka luo taskun. Taskulla on törmäyksentunnistus
    public void LuoTasku(Vector sijainti, double kallistus)
    {
        PhysicsObject taskuCollision = new PhysicsObject(128, 64)
        {
            Color = Color.Transparent,
            Position = sijainti,
            Shape = Shape.Rectangle,
            Angle = Angle.FromDegrees(kallistus),
            Tag = "taskucollision"
        };
        taskuCollision.MakeStatic();
        Add(taskuCollision);
#if DEBUG
        taskuCollision.Color = Color.Pink;
#endif

    }


    // Funktio joka luo näkymättömät sivulaidat törmäystä varten
    public void LuoLaita(double leveys, double korkeus, Vector sijainti)
    {
        PhysicsObject laita = new PhysicsObject(leveys, korkeus)
        {
            Shape = Shape.Rectangle,
            Position = sijainti,
            Color = Color.Transparent,
            Tag = "laita"
        };
        laita.MakeStatic();
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
        Add(maila,2);

        Timer mailanAjastin = new Timer
        {
            Interval = 0.016
        };

        mailanAjastin.Timeout += delegate ()
        {
            if (Math.Abs(valkoinenPallo.Velocity.X) > 0.9 || Math.Abs(valkoinenPallo.Velocity.Y) > 0.9)
            {
                maila.Size = new Vector(1, 1);
                CANHIT = false;
            }
            else
            {
                maila.Size = new Vector(17, 328);
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
        maila.Angle = Angle.FromRadians(Math.Atan2(posY, posX) + Math.PI / 2);
    }

    public void LyoPalloa(PhysicsObject pallo, PhysicsObject maila, ref double voima)
    {
        Vector suunta = new Vector(pallo.X - maila.X, pallo.Y - maila.Y);
        if (CANHIT == true) { 
            pallo.Push(suunta.Normalize() * voima);
            HITCOUNTER++;
            MessageDisplay.Add(HITCOUNTER.ToString());
        }
        else
        {
            Sfx.PlayError();
        }
        pallo.LinearDamping = 0.99;
    }

    // Alustaa pelin
    public void Init(PhysicsObject maila, PhysicsObject valkoinenPallo)
    {
        LuoKentta();
        LuoValkoinenPallo(valkoinenPallo);
        LuoMaila(maila, valkoinenPallo);
        LuoOhjaimet(maila, valkoinenPallo);
        Sfx.PlayMusic();
    }
    // Tätä kutsutaan kun käyttäjä painaa r-kirjainta ja resetoi pelin. Suorittaa clearallin ja ajaa tarvittavat aliohjelmat uudestaan
    public void Reset(PhysicsObject maila, PhysicsObject valkoinenPallo)
    {
        ClearAll();
        Sfx.StopMusic();
        Init(maila, valkoinenPallo);
    }
}


// TODO LISÄÄ LYÖNTIÄÄNI JA PALLOJEN TÖRMÄYS, EHKÄ TASKUTÖRMÄYSÄÄNI
public class SFX
{
    readonly SoundEffect error = Game.LoadSoundEffect("false");
    readonly SoundEffect seina = Game.LoadSoundEffect("wall");
    readonly SoundEffect intro = Game.LoadSoundEffect("intro");
    readonly SoundEffect bg = Game.LoadSoundEffect("bg");

    public void PlayWall()
    {
        seina.Play(0.1, 0, 0);
    }
    public void PlayError()
    {
        error.Play(0.2, 0, 0);
    }

    public void PlayMusic()
    {
        intro.Play(0.3, 0, 0);
        Timer.SingleShot(6.79, delegate { bg.Play(0.3, 0, 0); });
        Timer.CreateAndStart(143.8, delegate { bg.Play(0.3, 0, 0); });
    }

    public void StopMusic()
    {
        intro.Stop();
        bg.Stop();
    }

}