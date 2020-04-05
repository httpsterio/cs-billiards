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
        PhysicsObject valkoinenPallo = new PhysicsObject(24,24);
        PhysicsObject maila = new PhysicsObject(17, 328);
        PhysicsObject tasku = new PhysicsObject(80,80);

        // Kutsutaan tarvittavat aliohjelmat
        LuoKentta();
        LuoValkoinenPallo(valkoinenPallo);
        LuoMaila(maila, valkoinenPallo);
        LuoOhjaimet(maila, valkoinenPallo);

        void seinatormays(PhysicsObject pallo, PhysicsObject kohde)
        {
            if (kohde.Tag.ToString() == "laita")
            {
                kohde.Color = RandomGen.NextColor();
            }
        }

        AddCollisionHandler(valkoinenPallo, seinatormays);

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

        // Lataa kuvasta kentän grafiikat.
        GameObject kentta = new GameObject(784,448);
        kentta.Image = LoadImage("poyta");
        kentta.Shape = Shape.Rectangle;
        kentta.Position = new Vector(0,0);
        Add(kentta,-1);


        // Pystysuuntaiset laidat
        LuoLaita(64, 312, new Vector(-350-11-16, 0));
        LuoLaita(64, 312, new Vector(350+11+16, 0));

        // Sivusuuntaiset laidat
        LuoLaita(296, 64, new Vector(-176, 209));
        LuoLaita(296, 64, new Vector(-176, -209));
        LuoLaita(296, 64, new Vector(176, 209));
        LuoLaita(296, 64, new Vector(176, -209));

        // Nurkkataskut
        LuoTasku(new Vector(-376, 200), 45);
        LuoTasku(new Vector(376, 200), -45);
        LuoTasku(new Vector(-376, -200), -45);
        LuoTasku(new Vector(376, -200), 45);

        // Keskitaskut
        LuoTasku(new Vector(0, 228), 0);
        LuoTasku(new Vector(0, -228), 0);

    }

    public void LuoTasku(Vector sijainti, double kallistus)
    {
        PhysicsObject taskuCollision = new PhysicsObject(128, 64);
        taskuCollision.Color = Color.Pink;
        taskuCollision.Position = sijainti;
        taskuCollision.MakeStatic();
        taskuCollision.Shape = Shape.Rectangle;
        taskuCollision.Angle = Angle.FromDegrees(kallistus);
        taskuCollision.Tag = "taskucollision";
        Add(taskuCollision);
    }


    // Funktio joka luo näkymättömät sivulaidat törmäystä varten
    public void LuoLaita(double leveys, double korkeus, Vector sijainti) {
        PhysicsObject laita = new PhysicsObject(leveys, korkeus);
        laita.Shape = Shape.Rectangle;
        laita.Position = sijainti;
        laita.MakeStatic();
        laita.Color = Color.Black;
        laita.Tag = "laita";
        Add(laita);
        
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
            if (Math.Abs(valkoinenPallo.Velocity.X) > 0.5 || Math.Abs(valkoinenPallo.Velocity.Y) > 0.5)
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
