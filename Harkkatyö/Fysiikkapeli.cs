using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;


/// @author  Sami Singh
/// @version 19.4.2020
///
/// <summary>
/// Jypelillä toteutettu biljardi-peli yhdelle pelaajalle
/// </summary>
public class Harkkatyö : PhysicsGame
{
    // Muutama yleinen vakio, siirrän mahdolisuuksien mukaan jonnekkin aliohjelmaan, mikäli arvoja ei tarvitse kuljettaa joka aliohjelman välillä
    // Boolean arvo voiko pelaaja lyödä vai ei, asetetaan negatiiviseksi lyönnin yhteydessä ja tarkistetaan 60 kertaa sekunnissa liikkuvatko pallot vielä,
    // Kunnes pallot eivät enää liiku, asetetaan takaisin trueksi.
    private bool CANHIT = true;

    // Laskuri montako kertaa pelaaja on lyönyt, käytetään pisteytyksessä. Voisi mahdollisesti siirtää aliohjelmaankin.
    private int POINTSCOUNTER = 0;

    // Lista palloista jotka ovat pelissä
    private List<PhysicsObject> PALLOTPELISSA = new List<PhysicsObject>();

    // Ääniluokan alustus
    private SFX Sfx;


    public override void Begin()
    {
        // Asetetaan ääniluokka objektiin
        Sfx = new SFX();
        Mouse.IsCursorVisible = true;


        // Alustetaan fysiikkaolioita. Näitä tarvitaan eri aliohjelmien välillä ja niitä välitetään parametrina toisilleen, muutoin nämä olisivat aliohjelmissaan.
        PhysicsObject valkoinenPallo = new PhysicsObject(16, 16);
        PhysicsObject maila = new PhysicsObject(17, 328);

        // Kutsutaan tarvittavat aliohjelmat alkuun. Yritin tässä kutsua Init()-metodia, mutta pelissä esiintyy bugeja kontrollien kanssa tällöin.
        LuoKentta();
        LuoValkoinenPallo(valkoinenPallo);
        LuoMaila(maila);
        LuoOhjaimet(maila, valkoinenPallo);
        LisaaPallot(PalloInit());
        Tormaykset(valkoinenPallo, PallotPelissa);
        Updater(valkoinenPallo);
        Sfx.PlayMusic();

    }

    /// <summary>
    /// 60 FPS päivittyvä ajastin jossa voi suorittaa jatkuvasti tarkistettavia asioita.
    /// </summary>
    /// <param name="pallo">Vataanottaa valkoisen pelipallon</param>
    private void Updater(PhysicsObject pallo)
    {
        Timer.CreateAndStart(0.016, getBallVelocity);
        void getBallVelocity()
        {
            if (Math.Abs(pallo.Velocity.X) > 1 || Math.Abs(pallo.Velocity.Y) > 1)
            {
                CanHit = false;
            }
            else
            {
                CanHit = true;
            }
        }
    }


    public Boolean CanHit
    {
        get { return CANHIT; }
        set { CANHIT = value; }
    }

    public List<PhysicsObject> PallotPelissa
    {
        get { return PALLOTPELISSA; }
        set { PALLOTPELISSA = value; }
    }


    public int PointsCounter
    {
        get { return POINTSCOUNTER; }
        set { POINTSCOUNTER = value; }
    }



    /// <summary>
    /// Aliohjelma joka on vastuussa erilaisista törmäyksenkäsittelyistä
    /// </summary>
    /// <param name="pelipallo">Beginissä alustettu valkoinen pallo</param>
    /// <param name="pallolista">Lista pelissä olevista palloista</param>
    private void Tormaykset(PhysicsObject pelipallo, List<PhysicsObject> pallolista)
    {
        // Törmäyskäsittelijä joka toistaa ääniluokasta äänen kun objekti törmää joko laitaan tai kulmaan
        void palloSeinaAani(PhysicsObject pallo, PhysicsObject kohde)
        {
            if (kohde.Tag.ToString() == "laita")
            {
                Sfx.PlayWall();
            }
        }

        // Kun pelipallo törmää toiseen palloon, toistetaan tämä ääni
        void palloPalloAani(PhysicsObject pallo, PhysicsObject kohde)
        {
            String kohdepallo = kohde.Tag.ToString();
            bool isBall = kohdepallo.Contains("p");
            if (isBall == true)
            {
                Sfx.PlayBall();
            }
        }

        // Funktio joka käsittelee kun pelipallo joutuu pussitettavaksi
        void valkoinenTasku(PhysicsObject pallo, PhysicsObject kohde)
        {
            if (kohde.Tag.ToString() == "taskucollision")
            {
                Sfx.PlayFail();
                MessageDisplay.Add("Valkoinen taskussa");
                PointsCounter -= 5;
                pallo.Velocity = new Vector(0, 0);
                pallo.Position = new Vector(10000, 0);
                Timer palloTaskussa = new Timer
                {
                    Interval = 0.16
                };
                palloTaskussa.Timeout += (delegate {
                if (CanHit == false){}
                else
                    {
                        pallo.Position = new Vector(200, 0);
                        palloTaskussa.Stop();
                    }
                });
                palloTaskussa.Start();
            }
        }

        // Funktio joka toistaa äänen kun tavallinen pallo osuu taskuun
        void palloTasku(PhysicsObject pallo, PhysicsObject kohde)
        {
            if (kohde.Tag.ToString() == "taskucollision")
            {
                Sfx.PlayWin();
                MessageDisplay.Add("pallo taskussa");
                
                pallo.Destroy();
                PointsCounter += 1;
            }
        }

        // Lisätään ylläoleva törmäyksenkäsittelijä valkoiseen palloon
        AddCollisionHandler(pelipallo, palloSeinaAani);

        // Lisätään jokaiselle tavalliselle pallolle ääniefekti törmätessään seinään
        pallolista.ForEach(pallo => { AddCollisionHandler(pallo, palloSeinaAani); });

        // Ääniefekti kun pelipallo ja muut pallot törmäävät toisiinsa
        AddCollisionHandler(pelipallo, palloPalloAani);

        // Tapahtumankäsittelijä kun pelipallo joutuu pussitettavaksi
        AddCollisionHandler(pelipallo, valkoinenTasku);

        // Lisätään jokaiselle tavalliselle pallolle törmäyksentunnistus
        pallolista.ForEach(pallo => { AddCollisionHandler(pallo, palloTasku); });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private List<PhysicsObject> PalloInit()
    {
        double r = 8;
        double pyth = Math.Sqrt(Math.Pow(2 * r, 2) - Math.Pow(r, 2));
        double sp = -150;
        var objektiLista = new List<PhysicsObject>();
        var palloLista = new List<(double, double, String)>
        {
            (sp-0, 0, "p1"),

            (sp-pyth, (r), "p7"),
            (sp-pyth, -(r), "p12"),

            (sp-(2 * pyth), (2*r), "p15"),
            (sp-(2*pyth), 0, "p8"),
            (sp-(2*pyth), -(2*r), "p1"),

            (sp-(3*pyth), (3*r), "p6"),
            (sp-(3*pyth), (1*r), "p10"),
            (sp-(3*pyth), -(1*r), "p3"),
            (sp-(3*pyth), -(3*r), "p14"),

            (sp-(4*pyth), (4*r), "p11"),
            (sp-(4*pyth), (2*r), "p2"),
            (sp-(4*pyth), 0, "p13"),
            (sp-(4*pyth), -(2*r), "p4"),
            (sp-(4*pyth), -(4*r), "p5"),
        };

        foreach (var item in palloLista)
        {
            objektiLista.Add(LuoPallo(item.Item1, item.Item2, r, item.Item3));
        }

        static PhysicsObject LuoPallo(double x, double y, double r, String nimi)
        {
            PhysicsObject pallo = new PhysicsObject(r * 2, r * 2)
            {
                X = x,
                Y = y,
                Tag = nimi,
                Shape = Shape.Circle,
                Color = Color.Transparent,
                Image = LoadImage(nimi),
                LinearDamping = 0.98,
                Mass = 0.04
            };
            return pallo;
        }

        return objektiLista;

    }

    /// <summary>
    /// Hakee pallot 
    /// </summary>
    /// <param name="palloinit"></param>
    private void LisaaPallot(List<PhysicsObject> palloinit)
    {
        List<PhysicsObject> pallolista = new List<PhysicsObject>();
        foreach (var item in PallotPelissa)
        {
            pallolista.Add(item);
        }

        foreach (var pallo in palloinit)
        {
            Add(pallo, 1);
            pallolista.Add(pallo);
        }

        PallotPelissa = pallolista;
    }

    /// <summary>
    /// Sitoo hiiren näppäimet ja näppäimistön painikkeet pelin toimintoihin
    /// </summary>
    /// <param name="maila">Beginissä alustettu maila</param>
    /// <param name="valkoinenPallo">Beginissä alustettu valkoinen pallo</param>
    private void LuoOhjaimet(PhysicsObject maila, PhysicsObject valkoinenPallo)
    {
        double voimaDefault = 700;
        double voima = voimaDefault;
        double voimaMax = 10000;

        // Sitoo pelimailan hiireen ja liikuttaa pelimailaa SiirraMaila-ohjelman avulla
        Mouse.ListenMovement(0.1, SiirraMaila, "liikuta mailaa", maila, valkoinenPallo);

        // Kun hiiren vasen painike on painettuna, kasvattaa voimakerrointa 23:lla sekunnissa kunhan se on alle voimaMaxin arvon
        Mouse.Listen(MouseButton.Left, ButtonState.Down, delegate ()
        {
            if (voima <= voimaMax)
            {
                voima += 23;
            }
            else
            {
                voima = voimaMax;
            }
        }, null);

        // Kun hiiren vasen painike on painettu kerran (mutta ei vielä päästetty irti) toistetaan ääniklippiä
        Mouse.Listen(MouseButton.Left, ButtonState.Pressed, delegate () {
            Sfx.PlayPower();
        }, null);

        // Kun hiiren painikkeesta on päästetty irti, pysäyttää ääniklipin ja välittää voimakertoimen arvon LyoPalloa-funktiolle. Sen jälkeen voimakerroin asetetaan takaisin oletusarvoonsa.
        Mouse.Listen(MouseButton.Left, ButtonState.Released, delegate () {
            Sfx.StopPower();
            LyoPalloa(valkoinenPallo, maila, ref voima);
            voima = voimaDefault;
        }, "Lyö palloa");


        // Sitoo Esc-näppäimen pelin sulkemiseen
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        // Sitoo R-näppäimen pelin uudelleenkäynnistykseen ja suorittaa Reset-aliohjelman.
        Keyboard.Listen(Key.R, ButtonState.Pressed, delegate ()
        {
            Reset(maila, valkoinenPallo);
        }, "Resetoi peli");

    }

    /// <summary>
    /// Luo pelaajan valkoisen pallon
    /// </summary>
    /// <param name="valkoinenPallo">Pääohjelmassa alustettu fysiikkaolio välitetään tänne ja lisätään peliin</param>
    private void LuoValkoinenPallo(PhysicsObject valkoinenPallo)
    {
        valkoinenPallo.Shape = Shape.Circle;
        valkoinenPallo.X = 200;
        valkoinenPallo.Tag = "pv";
        valkoinenPallo.Y = 0;
        valkoinenPallo.Mass = 0.1;
        Add(valkoinenPallo);
        valkoinenPallo.Velocity = new Vector(0, 0);
    }

    /// <summary>
    /// Luo pelikentän joka koostuu kolmesta gameobjektista joilla on kuvat (ei fysiikoita) sekä lisää näkymättömiä fysiikkaolioita sekä törmäyksentunnistukseen sekä pelikentän pelilaidoiksi
    /// </summary>
    private void LuoKentta()
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

        // Lista vektoreista sekä kallistuskulmista joista luodaan taskuja
        var taskuLista = new List<(Vector, double)>
        {
            (new Vector(-372, 196), 45),
            (new Vector(372, 196), -45),
            (new Vector(-372, -196), -45),
            (new Vector(372, -196), 45),
            (new Vector(0, 224), 0),
            (new Vector(0, -224), 0)
        };

        // Iteroi listan läpi ja välittää listan arvot parametrina funktiolle joka luo taskut törmäyksiä varten 
        foreach (var item in taskuLista)
        {
            LuoTasku(item.Item1, item.Item2);
        }
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

    /// <summary>
    /// Luo pelaajan ohjaamaa mailaa ja tarkistaa voiko lyödä ja säätää sen perusteella mailan kokoa
    /// </summary>
    /// <param name="maila">Vastaanottaa parametrina Beginissä alustettu maila</param>
    public void LuoMaila(PhysicsObject maila)
    {
        Vector paikkaruudulla = Mouse.PositionOnScreen;
        maila.Color = Color.Transparent;
        maila.Shape = Shape.Rectangle;
        maila.Image = LoadImage("maila");
        maila.X = paikkaruudulla.X;
        maila.Y = paikkaruudulla.Y;
        maila.Angle = Angle.FromDegrees(0);
        maila.IgnoresCollisionResponse = true;
        Add(maila, 2);

        // Ajastin joka päivittyy 60FPS
        Timer.CreateAndStart(0.016, mailanKoko);

        // Säätää mailan kokoa CanHitin perusteella, jos ei voi lyödä niin maila piiloitetaan.
        void mailanKoko()
        {
            if (CanHit == true)
            {
                maila.Size = new Vector(17, 328);
            }
            else
            {
                maila.Size = new Vector(1, 1);
            }
        }

    }

    /// <summary>
    /// Hiireen sidottu funktio, laskee Atan2:lla mailan ja pallon välisen kulman ja kääntää mailaa osoittamaan pallon suuntaan aina
    /// </summary>
    /// <param name="maila">Vastaanottaa beginnissä alustettu maila</param>
    /// <param name="pallo">Vastaanottaa beginissä alustettu valkoinen pallo</param>
    public void SiirraMaila(PhysicsObject maila, PhysicsObject pallo)
    {
        maila.Position = Mouse.PositionOnScreen;
        double posX = maila.Position.X - pallo.Position.X;
        double posY = maila.Position.Y - pallo.Position.Y;
        maila.Angle = Angle.FromRadians(Math.Atan2(posY, posX) + Math.PI / 2);
    }

    /// <summary>
    /// Hiireen sidottu funktio joka vastaa palloon lyömisestä
    /// </summary>
    /// <param name="pallo"></param>
    /// <param name="maila"></param>
    /// <param name="voima"></param>
    public void LyoPalloa(PhysicsObject pallo, PhysicsObject maila, ref double voima)
    {
        Vector suunta = new Vector(pallo.X - maila.X, pallo.Y - maila.Y);
        if (CanHit == true)
        {
            pallo.Push(suunta.Normalize() * voima);
            PointsCounter--;
            MessageDisplay.Add(PointsCounter.ToString());
        }
        else
        {
            Sfx.PlayError();
        }
        pallo.LinearDamping = 0.98;
    }

    /// <summary>
    /// Alustaa pelin suorittamalla tarpeelliset funktiot resetin jälkeen.
    /// </summary>
    /// <param name="maila">Beginnissä alustettu maila</param>
    /// <param name="valkoinenPallo">Beginissä alustettu pallo</param>
    public void Init(PhysicsObject maila, PhysicsObject valkoinenPallo, List<PhysicsObject> PalloLista)
    {
        LuoKentta();
        LuoValkoinenPallo(valkoinenPallo);
        LuoMaila(maila);
        LuoOhjaimet(maila, valkoinenPallo);
        LisaaPallot(PalloLista);
        Updater(valkoinenPallo);
        Sfx.PlayMusic();
        PointsCounter = 0;
    }

    /// <summary>
    /// Uudelleenkäynnistää pelin, pysäyttää musiikin ja suorittaa Init-funktion
    /// </summary>
    /// <param name="maila">Beginissä alustettu maila</param>
    /// <param name="valkoinenPallo">Beginissä alustettu valkoinen pallo</param>
    public void Reset(PhysicsObject maila, PhysicsObject valkoinenPallo)
    {
        ClearAll();
        Sfx.StopMusic();
        Init(maila, valkoinenPallo, PalloInit());
    }
}
