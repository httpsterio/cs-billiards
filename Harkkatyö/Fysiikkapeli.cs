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
/// <bugs>
/// 1. BallsInGame ei ehkä päivity Updaterissa eikä lakkaa tarkistamasta jo-poistettuja palloja. Ei ole ongelma nyt kun poistettavan pallon velocity asetetaan 0:ksi ennen poistoa, mutta saattaa olla ongelma.
/// </bugs>
public class Harkkatyö : PhysicsGame
{
    // Muutama yksityinen muuttuja, siirrän mahdolisuuksien mukaan jonnekkin aliohjelmaan, mikäli arvoja ei tarvitse kuljettaa joka aliohjelman välillä

    // Boolean arvo voiko pelaaja lyödä vai ei, asetetaan negatiiviseksi lyönnin yhteydessä ja tarkistetaan 60 kertaa sekunnissa liikkuvatko pallot vielä, kunnes pallot eivät enää liiku, asetetaan takaisin trueksi.
    private bool CANHIT = true;

    // Laskuri montako kertaa pelaaja on lyönyt, käytetään pisteytyksessä. Voisi mahdollisesti siirtää aliohjelmaankin.
    private int POINTSCOUNTER = 0;

    // Lista palloista jotka ovat pelissä, joita voidaan get/set 
    private List<PhysicsObject> BALLSINGAME = new List<PhysicsObject>();

    // Ääniluokan alustus
    private SFX Sfx;

    /// <summary>
    /// Pelin pääohjelma jossa kutsutaan tarvittavat aliohjelmat
    /// </summary>
    public override void Begin()
    {
        // Asetetaan ääniluokka objektiin
        Sfx = new SFX();

        // Otetaan hiiri käyttöön
        Mouse.IsCursorVisible = true;


        // Alustetaan fysiikkaolioita. Näitä tarvitaan eri aliohjelmien välillä ja niitä välitetään parametrina toisilleen, muutoin nämä olisivat aliohjelmissaan.
        PhysicsObject valkoinenPallo = new PhysicsObject(16, 16);
        PhysicsObject maila = new PhysicsObject(17, 328);

        // Kutsutaan tarvittavat aliohjelmat alkuun. Yritin tässä kutsua Init()-metodia suoraan, mutta se oli ongelmallista. CollisionHandlerit esim. eivät resetoiduja kontrollien kanssa tulee tuplabindauksia johon ohjelma hajoaa.
        LuoKentta();
        LuoValkoinenPallo(valkoinenPallo);
        LuoMaila(maila);
        BindControls(maila, valkoinenPallo);
        AddBalls(BallInitList());
        Collisions(valkoinenPallo, BallsInGame);
        Updater(valkoinenPallo);
        Sfx.PlayMusic();

    }

    /// <summary>
    /// 60 FPS päivittyvä ajastin jossa voi suorittaa jatkuvasti tarkistettavia asioita.
    /// </summary>
    /// <param name="pallo">Vataanottaa valkoisen pelipallon</param>
    private void Updater(PhysicsObject whiteBall)
    {
        Timer.CreateAndStart(0.016, delegate { BallVelocity(); });

        // Asettaa CANHIT-booleanin tilaa riippuen pallojen velocitysta
        void BallVelocity()
        {
            // Tarkistaa valkoisen pallon velocityn
            if (Math.Abs(whiteBall.Velocity.X) > 1 || Math.Abs(whiteBall.Velocity.Y) > 1)
            {
                CanHit = false;
            }
            else
            {
                CanHit = true;
            }

            // Tarkistaa muiden pallojen velocityn
            // BallsInGame pitää ehkä lukea sisään paikalliseen muuttujaan
            foreach (var regularBall in BallsInGame)
            {
                if (regularBall.Velocity.X > 1 || regularBall.Velocity.Y > 1)
                {
                    CanHit = false;
                }
            }
        }
    }

    /// <summary>
    /// Julkinen boolean jolla tarkistetaan voiko pelaaja lyödä
    /// </summary>
    public Boolean CanHit
    {
        get { return CANHIT; }
        set { CANHIT = value; }
    }

    /// <summary>
    /// Julkinen lista pelissä olevista palloista
    /// </summary>
    public List<PhysicsObject> BallsInGame
    {
        get { return BALLSINGAME; }
        set { BALLSINGAME = value; }
    }

    /// <summary>
    /// Julkinen pistelaskurimuuttuja
    /// </summary>
    public int PointsCounter
    {
        get { return POINTSCOUNTER; }
        set { POINTSCOUNTER = value; }
    }


    /// <summary>
    /// Aliohjelma joka on vastuussa erilaisista törmäyksenkäsittelyistä
    /// </summary>
    /// <param name="whiteBall">Beginissä alustettu valkoinen pallo</param>
    /// <param name="ballList">Lista pelissä olevista palloista</param>
    private void Collisions(PhysicsObject whiteBall, List<PhysicsObject> ballList)
    {
        // Törmäyskäsittelijä joka toistaa ääniluokasta äänen kun objekti törmää laitaan
        void wallSound(PhysicsObject collidingBall, PhysicsObject collisionTarget)
        {
            if (collisionTarget.Tag.ToString() == "edge")
            {
                Sfx.PlayWall();
            }
        }

        // Kun pelipallo törmää toiseen palloon, toistetaan tämä ääni
        void ballSound(PhysicsObject whiteBall, PhysicsObject collisionTarget)
        {
            // Muunnetaan pallon tagi stringiksi ja tarkistetaan sisältääkö stringi kirjaimen p niinkuin pallo. 
            String collidingBall = collisionTarget.Tag.ToString();
            bool isBall = collidingBall.Contains("p");
            if (isBall == true)
            {
                Sfx.PlayBall();
            }
        }

        // Funktio joka käsittelee kun valkoinen pallo joutuu pussitettavaksi
        void whiteBallPocket(PhysicsObject whiteBall, PhysicsObject collisionTarget)
        {
            if (collisionTarget.Tag.ToString() == "pocketCollision")
            {
                Sfx.PlayFail();
                MessageDisplay.Add("Valkoinen taskussa");
                PointsCounter -= 5;

                // Pysäyttää valkoisen pallon ja siirtää sen ruudun ulkopuolelle (jotta pallo ei liiku kun se palautetaan=
                whiteBall.Velocity = new Vector(0, 0);
                whiteBall.Position = new Vector(10000, 0);

                Timer ballPocketed = new Timer
                {
                    Interval = 0.16
                };

                // Jos voidaan lyödä, siirrä pallo takaisin pöydälle ja pysäytä ajastin
                ballPocketed.Timeout += (delegate {
                    if (CanHit == true){
                        whiteBall.Position = new Vector(200, 0);
                        ballPocketed.Stop();
                    }
                });

                // Käynnistä ajastin pallon palauttamiseksi
                ballPocketed.Start();
            }
        }

        // Funktio joka käsittelee muiden pallojen pussitetuksijoutumisen
        void ballPocket(PhysicsObject collidingBall, PhysicsObject collisionTarget)
        {
            // Vie pelin pallot väliaikaisesti uuteen listaan
            List<PhysicsObject> removeBallList = new List<PhysicsObject>();
            BallsInGame.ForEach(ball => removeBallList.Add(ball));

            if (collisionTarget.Tag.ToString() == "pocketCollision")
            {
                // Jos pussitettava pallo on kasipallo ja palloja on muitakin jäljellä, suorittaa Fail();
                if (BallsInGame.Count > 1 && collidingBall.Tag.ToString() == "p8")
                {
                    Fail();
                }
                // Mikäli pussitettava pallo on kasipallo ja se on viimeinen pallo, suorittaa Win();
                if (BallsInGame.Count == 1 && collidingBall.Tag.ToString() == "p8")
                {
                    Win();
                }
                Sfx.PlayWin();
     
                // Oikeasti? Enkö voi lambdalla poistaa suoraan?
                // Poistaa väliaikaisesta listasta pallon tagin osoittamasta indeksistä
                removeBallList.RemoveAt(removeBallList.FindIndex(ball => ball.Tag == collidingBall.Tag));

                // Vie uuden listan yleiselle listamuuttujalle
                BallsInGame = removeBallList;

                // Asetetaan pussitetun pallon velocity nollaan, jostain syystä 
                collidingBall.Velocity = new Vector(0, 0);
                collidingBall.Destroy();
                PointsCounter += 5;
            }
        }

        // Lisätään ylläoleva törmäyksenkäsittelijä valkoiseen palloon
        AddCollisionHandler(whiteBall, wallSound);

        // Lisätään jokaiselle tavalliselle pallolle ääniefekti törmätessään seinään
        ballList.ForEach(regularBall => { AddCollisionHandler(regularBall, wallSound); });

        // Ääniefekti kun pelipallo ja muut pallot törmäävät toisiinsa
        AddCollisionHandler(whiteBall, ballSound);

        // Tapahtumankäsittelijä kun pelipallo joutuu pussitettavaksi
        AddCollisionHandler(whiteBall, whiteBallPocket);

        // Lisätään jokaiselle tavalliselle pallolle törmäyksentunnistus
        ballList.ForEach(pallo => { AddCollisionHandler(pallo, ballPocket); });
    }

    //void LuoPistelaskuri()
    //{
    //    pisteLaskuri = new IntMeter(0);

    //    Label pisteNaytto = new Label();
    //    pisteNaytto.X = Screen.Left + 100;
    //    pisteNaytto.Y = Screen.Top - 100;
    //    pisteNaytto.TextColor = Color.Black;
    //    pisteNaytto.Color = Color.White;

    //    pisteNaytto.BindTo(pisteLaskuri);
    //    Add(pisteNaytto);
    //}

    /// <summary>
    /// Alustaa pallojen luomisen generoimalla listasta joukon fysiikkaobjekteja antaen niillä sijainnin, mutta ei lisää peliin vielä
    /// </summary>
    /// <returns>Palauttaa List<PhysicsObject> jossa on generoitu alkuun tarvittavat pelipallot</returns>
    private List<PhysicsObject> BallInitList()
    {
        // Pallojen koko /2, tätä käytetään pallojen asemoinnissa. Pallojen etäisyys toisistaan y-akselilla on aina 2 * R,
        const double r = 8;

        // Apumuuttuja jossa lasketaan pythagoraksen kaavan avulla tangentin pituus. Pallorivit ovat x-akselilla hieman limittäin
        double pyth = Math.Sqrt(Math.Pow(2 * r, 2) - Math.Pow(r, 2));

        // Aloituspiste x-akselilla pelipalloille
        const double sp = -150;

        // Alustetaan uusi tyhjä lista jonne lisätään luotuja objekteja
        var objectList = new List<PhysicsObject>();

        // Lista määreistä jossa ensimmäinen arvo on tulevan pallon sijainti x-akselilla, toinen arvo on pallon sijainti y-akselilla ja kolmas stringi on pallon tuleva tag
        var ballList = new List<(double, double, String)>
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

        // Iteroi ylläolevan listan läpi ja kutsuu createBallia listan itemit parametrina
        foreach (var item in ballList)
        {
            objectList.Add(createBall(item.Item1, item.Item2, r, item.Item3));
        }

        // Paikallinen funktio jota kutsutaan pallojen luomiseksi listan määreiden perusteella
        static PhysicsObject createBall(double x, double y, double r, String nimi)
        {
            // Asettaa pallojen kooksi kooridinaateissakin hyödynnettyä säde-muuttujaa r * 2.
            PhysicsObject ball = new PhysicsObject(r * 2, r * 2)
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
            return ball;
        }

        // Palauttaa ylemmässä foreachissa iteroidut pallot listassa
        return objectList;

    }

    /// <summary>
    /// Käy parametrina saadun pallolistan läpi ja lisää ne peliin sekä antaa attribuutille päivitetyn listan pelissä olevista palloista
    /// </summary>
    /// <param name="ballInitList">Ottaa parametrina BallInitList() palautus eli lista fysiikkaobjekteista</param>
    private void AddBalls(List<PhysicsObject> ballInitList)
    {
        // Luo uuden paikallisen listan johon luetaan BallsInGame-attribuutista arvot (mikäli niitä jostain syystä siellä ensin olisi)
        List<PhysicsObject> ballList = new List<PhysicsObject>();
        foreach (var item in BallsInGame)
        {
            ballList.Add(item);
        }

        // Paramtrina saatu pallolista lisätään peliin ja lisätään paikalliseen listaan
        foreach (var ball in ballInitList)
        {
            Add(ball, 1);
            ballList.Add(ball);
        }

        // Paikallinen lista viedään päivitettynä takaisin attribuutille
        BallsInGame = ballList;
    }

    /// <summary>
    /// Sitoo hiiren näppäimet ja näppäimistön painikkeet pelin toimintoihin
    /// </summary>
    /// <param name="cue">Beginissä alustettu maila</param>
    /// <param name="whiteBall">Beginissä alustettu valkoinen pallo</param>
    private void BindControls(PhysicsObject cue, PhysicsObject whiteBall)
    {
        double hitPowerDefault = 300;
        double hitPower = hitPowerDefault;
        double hitPowerMax = 10000;

        // Sitoo pelimailan hiireen ja liikuttaa pelimailaa SiirraMaila-ohjelman avulla
        Mouse.ListenMovement(0.1, MoveCue, "Liikuta mailaa hiirellä", cue, whiteBall);

        // Kun hiiren vasen painike on painettuna, kasvattaa voimakerrointa 23:lla sekunnissa kunhan se on alle voimaMaxin arvon
        Mouse.Listen(MouseButton.Left, ButtonState.Down, delegate ()
        {
            if (hitPower <= hitPowerMax)
            {
                hitPower += 42.4;
                MessageDisplay.Add(hitPower.ToString());
            }
            else
            {
                hitPower = hitPowerMax;
                MessageDisplay.Add(hitPower.ToString());
            }
        }, null);

        // Kun hiiren vasen painike on painettu kerran (mutta ei vielä päästetty irti) toistetaan ääniklippiä
        Mouse.Listen(MouseButton.Left, ButtonState.Pressed, delegate () {
            Sfx.PlayPower();
        }, null);

        // Kun hiiren painikkeesta on päästetty irti, pysäyttää ääniklipin ja välittää voimakertoimen arvon LyoPalloa-funktiolle. Sen jälkeen voimakerroin asetetaan takaisin oletusarvoonsa.
        Mouse.Listen(MouseButton.Left, ButtonState.Released, delegate () {
            Sfx.StopPower();
            LyoPalloa(whiteBall, cue, ref hitPower);
            hitPower = hitPowerDefault;
            
        }, "Lyö palloa painamalla hiiren vasenta nappia. Mitä pidempään pidät nappia pohjassa, sen kovempaa lyöt.");


        // Sitoo Esc-näppäimen pelin sulkemiseen
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.F1, ButtonState.Down, ShowControlHelp, "Näytä näppäimet");

        // Sitoo R-näppäimen pelin uudelleenkäynnistykseen ja suorittaa Reset-aliohjelman.
        Keyboard.Listen(Key.R, ButtonState.Pressed, delegate ()
        {
            Reset(cue, whiteBall);
        }, "Aloita alusta");

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
        PhysicsObject pocketCollision = new PhysicsObject(128, 64)
        {
            Color = Color.Transparent,
            Position = sijainti,
            Shape = Shape.Rectangle,
            Angle = Angle.FromDegrees(kallistus),
            Tag = "pocketCollision"
        };
        pocketCollision.MakeStatic();
        Add(pocketCollision);
#if DEBUG
        pocketCollision.Color = Color.Pink;
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
            Tag = "edge"
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
    public void MoveCue(PhysicsObject maila, PhysicsObject pallo)
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

    public void Fail()
    {
        MessageDisplay.Add("FAIL FAIL FAIL");
        Sfx.StopMusic();
        Sfx.PlayGameOver();
        Pause();
    }

    public void Win()
    {
        MessageDisplay.Add("Voitto! Pisteesi ovat" + PointsCounter.ToString());
    }

    /// <summary>
    /// Alustaa pelin suorittamalla tarpeelliset funktiot resetin jälkeen.
    /// </summary>
    /// <param name="maila">Beginnissä alustettu maila</param>
    /// <param name="valkoinenPallo">Beginissä alustettu pallo</param>
    public void Init(PhysicsObject maila, PhysicsObject valkoinenPallo)
    {
        LuoKentta();
        LuoValkoinenPallo(valkoinenPallo);
        LuoMaila(maila);
        BindControls(maila, valkoinenPallo);
        AddBalls(BallInitList());
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
        // Asettaa jokaisen pallon velocityn nollaan, jotta resetin jälkeen CanHit päivittyy oikein.
        BallsInGame.ForEach(ball => ball.Velocity = new Vector(0, 0));
        ClearAll();
        Sfx.StopMusic();
        // Suorittaa uusiksi aliohjelmat, jotka tuhottiin ClearAllilla.
        Init(maila, valkoinenPallo);
    }
}
