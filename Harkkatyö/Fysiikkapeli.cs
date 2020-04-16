using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

public class Harkkatyö : PhysicsGame
{

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

    }

    public void Tormaykset(PhysicsObject tormaaja)
    {
        SoundEffect seinaAani = LoadSoundEffect("wall");

        void seinatormays(PhysicsObject pallo, PhysicsObject kohde)
        {
            if (kohde.Tag.ToString() == "laita")
            {
                seinaAani.Play(0.1, 0, 0);
            }
        }
        AddCollisionHandler(tormaaja, seinatormays);
    }
    public void LuoOhjaimet(PhysicsObject maila, PhysicsObject valkoinenPallo)
    {

        Mouse.ListenMovement(0.1, SiirraMaila, "liikuta mailaa", maila, valkoinenPallo);
        double voima = 10000;
        Mouse.Listen(MouseButton.Left, ButtonState.Pressed, delegate() { LyoPalloa(valkoinenPallo, maila, ref voima); MessageDisplay.Add(voima.ToString()); } , "Lyö palloa");
        Keyboard.Listen(Key.D1, ButtonState.Pressed, delegate() { voima = 10000; }, "Aseta lyönnin voimakkuus") ; // 1
        Keyboard.Listen(Key.D2, ButtonState.Pressed, AsetaVoima, "Aseta lyönnin voimakkuus", 2);
        Keyboard.Listen(Key.D3, ButtonState.Pressed, AsetaVoima, "Aseta lyönnin voimakkuus", 5);
        Keyboard.Listen(Key.D4, ButtonState.Pressed, delegate() { voima = 100000; }, "Aseta lyönnin voimakkuus"); // 10
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.R, ButtonState.Pressed, delegate()
        {
            ClearAll();
            LuoKentta();
            LuoValkoinenPallo(valkoinenPallo);
            LuoMaila(maila, valkoinenPallo);
            LuoOhjaimet(maila, valkoinenPallo);

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

        //// Nurkkataskut
        //LuoTasku(new Vector(-376, 200), 45);
        //LuoTasku(new Vector(376, 200), -45);
        //LuoTasku(new Vector(-376, -200), -45);
        //LuoTasku(new Vector(376, -200), 45);

        // Kulmataskujen "supistajat"
        LuoLaitaKulma(new Vector(-364, 162), 60);
        LuoLaitaKulma(new Vector(364, 162), -60);
        LuoLaitaKulma(new Vector(-364, -162), -60);
        LuoLaitaKulma(new Vector(364, -162), 60);

        LuoLaitaKulma(new Vector(-330, 196), 30);
        LuoLaitaKulma(new Vector(330, 196), -30);
        LuoLaitaKulma(new Vector(-330, -196), -30);
        LuoLaitaKulma(new Vector(330, -196), 30);

        //// Keskitaskut
        //LuoTasku(new Vector(0, 228), 0);
        //LuoTasku(new Vector(0, -228), 0);

    }

    public void LuoLaitaKulma(Vector sijainti, double kallistus)
    {
        PhysicsObject kulma = new PhysicsObject(10, 40);
        kulma.Shape = Shape.Rectangle;
        kulma.Position = sijainti;
        kulma.MakeStatic();
        kulma.Color = Color.Red;
        kulma.Tag = "kulma";
        kulma.Angle = Angle.FromDegrees(kallistus);
        Add(kulma, 3);

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
            }
            else
            {
                maila.Size = new Vector(17,328);

            }

        };
        mailanAjastin.Start();
    }

    public void LuoTasku(PhysicsObject tasku, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            tasku = new PhysicsObject(80,80);
            tasku.Shape = Shape.Diamond;
            tasku.Color = Color.Black;
            tasku.Position = RandomGen.NextVector(Level.BoundingRect);
            tasku.MakeStatic();
            Add(tasku);
        }
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
        pallo.Push(suunta.Normalize() * voima);
        pallo.LinearDamping = 0.985;
    }
}
