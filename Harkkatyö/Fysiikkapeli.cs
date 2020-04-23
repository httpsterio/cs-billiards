﻿using System;
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
/// 1. BallsInGame ei ehkä päivity Updaterissa eikä lakkaa tarkistamasta jo-poistettuja palloja. Ei ole ongelma nyt kun poistettavan pallon velocity asetetaan 0:ksi ennen poistoa, mutta saattaa olla ongelma. Ei lue ehkä myöskään uusia palloja resetin jälkeen?
/// </bugs>
public class Harkkatyö : PhysicsGame
{
    // Muutama yksityinen muuttuja, siirrän mahdolisuuksien mukaan jonnekkin aliohjelmaan, mikäli arvoja ei tarvitse kuljettaa joka aliohjelman välillä

    // Boolean arvo voiko pelaaja lyödä vai ei, asetetaan negatiiviseksi lyönnin yhteydessä ja tarkistetaan 60 kertaa sekunnissa liikkuvatko pallot vielä, kunnes pallot eivät enää liiku, asetetaan takaisin trueksi.
    private bool CANHIT = true;

    // Laskuri montako kertaa pelaaja on lyönyt, käytetään pisteytyksessä. Voisi mahdollisesti siirtää aliohjelmaankin.

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
        PhysicsObject whiteBall = new PhysicsObject(16, 16);
        PhysicsObject cue = new PhysicsObject(17, 328);

        // Kutsutaan tarvittavat aliohjelmat alkuun. Yritin tässä kutsua Init()-metodia suoraan, mutta se oli ongelmallista. CollisionHandlerit esim. eivät resetoiduja kontrollien kanssa tulee tuplabindauksia johon ohjelma hajoaa.
        CreateTable();
        CreateWhiteBall(whiteBall);
        CreateCue(cue);
        BindControls(cue, whiteBall);
        AddBalls(BallInitList());
        CreatePointCounter();
        Collisions(whiteBall, BallsInGame);
        Updater(whiteBall);
        Sfx.PlayMusic();

    }

    /// <summary>
    /// 60 FPS päivittyvä ajastin jossa voi suorittaa jatkuvasti tarkistettavia asioita.
    /// </summary>
    /// <param name="pallo">Vataanottaa valkoisen pelipallon</param>
    private void Updater(PhysicsObject whiteBall)
    {
        Timer.CreateAndStart(0.016, delegate { 
            BallVelocity();

        });


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
                pointCounter.Value -= 5;

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

                // Asetetaan pussitetun pallon velocity nollaan 
                collidingBall.Velocity = new Vector(0, 0);
                collidingBall.Destroy();
                pointCounter.Value += 5;
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

    // Attribuutti pistelaskurille
    private IntMeter pointCounter;

    // Funktio joka lisää peliin pistelaskurin
    /// <summary>
    /// Pistelaskuri joka on 
    /// </summary>
    void CreatePointCounter()
    {
        pointCounter = new IntMeter(0, int.MinValue, int.MaxValue);

        Label pointLabel = new Label
        {
            Y = Screen.Bottom + 30,
            X = 0,
            TextColor = Color.Black,
            Color = Color.White,
            Title = "Pisteesi"
        };
        pointLabel.BindTo(pointCounter);
        Add(pointLabel);
    }

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
        const double powerIncrement = 42.4;

        // Sitoo pelimailan hiireen ja liikuttaa pelimailaa SiirraMaila-ohjelman avulla
        Mouse.ListenMovement(0.1, MoveCue, "Liikuta mailaa hiirellä", cue, whiteBall);

        // Kun hiiren vasen painike on painettuna, kasvattaa hitPoweria powerIncrementin verran 60 kertaa sekunnissa.
        // powerIncrementin arvo on siis (((hitPowerMax - hitPowerDefault) / power.wavin kesto) / 60 FPS)
        Mouse.Listen(MouseButton.Left, ButtonState.Down, delegate ()
        {
            if (hitPower <= hitPowerMax)
            {
                hitPower += powerIncrement;
                #if DEBUG
                        MessageDisplay.Add(hitPower.ToString());
                #endif

            }
            else
            {
                hitPower = hitPowerMax;
                #if DEBUG
                        MessageDisplay.Add(hitPower.ToString());
                #endif
            }
        }, null);

        // Kun hiiren vasen painike on painettu kerran (mutta ei vielä päästetty irti) toistetaan ääniklippiä
        Mouse.Listen(MouseButton.Left, ButtonState.Pressed, delegate () {
            Sfx.PlayPower();
        }, null);

        // Kun hiiren painikkeesta on päästetty irti, pysäyttää ääniklipin ja välittää voimakertoimen arvon LyoPalloa-funktiolle. Sen jälkeen voimakerroin asetetaan takaisin oletusarvoonsa.
        Mouse.Listen(MouseButton.Left, ButtonState.Released, delegate () {
            Sfx.StopPower();
            HitBall(whiteBall, cue, ref hitPower);
            hitPower = hitPowerDefault;
            
        }, "Lyö palloa painamalla hiiren vasenta nappia. Mitä pidempään pidät nappia pohjassa, sen kovempaa lyöt.");


        // Sitoo Esc-näppäimen pelin sulkemiseen
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        // Sitoo F1-painikkeen ohjetekstin näyttämiseen
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
    /// <param name="whiteBall">Pääohjelmassa alustettu fysiikkaolio välitetään tänne ja lisätään peliin</param>
    private void CreateWhiteBall(PhysicsObject whiteBall)
    {
        whiteBall.Shape = Shape.Circle;
        whiteBall.X = 200;
        whiteBall.Tag = "pv";
        whiteBall.Y = 0;
        whiteBall.Mass = 0.1;
        whiteBall.LinearDamping = 0.98;
        Add(whiteBall);
        whiteBall.Velocity = new Vector(0, 0);
    }

    /// <summary>
    /// Luo pelikentän joka koostuu kolmesta gameobjektista joilla on kuvat (ei fysiikoita) sekä lisää näkymättömiä fysiikkaolioita sekä törmäyksentunnistukseen sekä pelikentän pelilaidoiksi
    /// </summary>
    private void CreateTable()
    {
        // Asettaa ikkunan koon, laittaa pelille laidat ja zoomaa pelin näkyviin elementteihin.
        SetWindowSize(1280, 1024);
        Level.CreateBorders();
        Level.BackgroundColor = new Color(4,64,72);
        Camera.ZoomToLevel();

        // Asettaa peliobjektille kentän kuvan ja asettaa sen tasolle -1 (pallojen alle)
        GameObject tableBorders = new GameObject(784, 448)
        {
            Image = LoadImage("poyta"),
            Shape = Shape.Rectangle,
            Position = new Vector(0, 0)
        };
        Add(tableBorders, -1);

        // Asettaa taskujen kuvan gameobjektille ja lisää sen tasolle -2 (laitojen ja pallojen alle)
        GameObject pockets = new GameObject(784, 448)
        {
            Image = LoadImage("taskut"),
            Shape = Shape.Rectangle,
            Position = new Vector(0, 0)
        };
        Add(pockets, -2);

        // Asettaa pelikentälle taustaobjektin tasolle -3
        GameObject cloth = new GameObject(784, 448)
        {
            Color = new Color(0,128,64),
            Shape = Shape.Rectangle,
            Position = new Vector(0, 0)
        };
        Add(cloth, -3);

        // Alempana on kolme listaa, jotka sisältävät koordinaatteja, kokoja ja mahdollisesti kallistuskulmia
        // joiden avulla generoidaan fysiikkaobjekteja (laidat, taskut ja taskujen supistajat). Alkuperäisessä 
        // ideassa Kenttä koostuu läpinäkyvistä kuvista ja kuvien tasoista voitaisiin generoida kaikki tarvittavat
        // ilman erillistä listaa, mutta Jypeli ei tue ns. onttoja kuvia, eli kentän laidat eivät voineet toimia
        // sisälaitoina, joten tässä on purkkaratkaisu. 

        // Lista, jossa on laitablokkien koot (korkeus, leveys) ja sijainti vektoreina. Näitä käytetään törmäyksissä ja elementit on listassa jotta niitä voidaan kutsua loopilla.
        var borderList = new List<(double, double, Vector)>
        {
            (64, 312, new Vector(-376, 0)),
            (64, 312, new Vector(376, 0)),
            (296, 64, new Vector(-176, 209)),
            (296, 64, new Vector(-176, -209)),
            (296, 64, new Vector(176, 209)),
            (296, 64, new Vector(176, -209))
        };

        // Iteroi listan läpi ja välittää listan itemeiden arvot parametrina funktiolle joka luo laidat
        foreach (var item in borderList)
        {
            CreateBorder(item.Item1, item.Item2, item.Item3);
        }

        // Lista vektoreista sekä kallistuskulmista joista luodaan taskuja
        var pocketList = new List<(Vector, double)>
        {
            (new Vector(-372, 196), 45),
            (new Vector(372, 196), -45),
            (new Vector(-372, -196), -45),
            (new Vector(372, -196), 45),
            (new Vector(0, 224), 0),
            (new Vector(0, -224), 0)
        };

        // Iteroi listan läpi ja välittää listan arvot parametrina funktiolle joka luo taskut törmäyksiä varten 
        foreach (var item in pocketList)
        {
            CreatePocket(item.Item1, item.Item2);
        }
    }

    /// <summary>
    /// Funktio joka luo taskun. Taskuja käytetään pallojen törmäyksen tunnistuksessa
    /// </summary>
    /// <param name="position">Taskun sijainti vektorina</param>
    /// <param name="angledegrees">taskun kallistus asteina</param>
    public void CreatePocket(Vector position, double angledegrees)
    {
        PhysicsObject pocketCollision = new PhysicsObject(128, 64)
        {
            Color = Color.Transparent,
            Position = position,
            Shape = Shape.Rectangle,
            Angle = Angle.FromDegrees(angledegrees),
            Tag = "pocketCollision"
        };
        pocketCollision.MakeStatic();
        Add(pocketCollision);
#if DEBUG
        pocketCollision.Color = Color.Pink;
#endif

    }

    /// <summary>
    /// Funktio joka luo näkymättömät sivulaidat pallojen kimmoitusta varten
    /// </summary>
    /// <param name="width">laidan leveys</param>
    /// <param name="height">laidan korkeus</param>
    /// <param name="position">laidan sijainti vektorina</param>
    public void CreateBorder(double width, double height, Vector position)
    {
        PhysicsObject border = new PhysicsObject(width, height)
        {
            Shape = Shape.Rectangle,
            Position = position,
            Color = Color.Transparent,
            Tag = "edge"
        };
        border.MakeStatic();
        Add(border);
#if DEBUG
        laita.Color = Color.Blue;
#endif

    }

    /// <summary>
    /// Luo pelaajan ohjaamaa mailaa ja tarkistaa voiko lyödä ja säätää sen perusteella mailan kokoa
    /// </summary>
    /// <param name="cue">Vastaanottaa parametrina Beginissä alustettu maila</param>
    public void CreateCue(PhysicsObject cue)
    {
        Vector paikkaruudulla = Mouse.PositionOnScreen;
        cue.Color = Color.Transparent;
        cue.Shape = Shape.Rectangle;
        cue.Image = LoadImage("maila");
        cue.X = paikkaruudulla.X;
        cue.Y = paikkaruudulla.Y;
        cue.Angle = Angle.FromDegrees(0);
        cue.IgnoresCollisionResponse = true;
        Add(cue, 2);

        // Ajastin joka päivittyy 60FPS
        Timer.CreateAndStart(0.016, cueSize);

        // Säätää mailan kokoa CanHitin perusteella, jos ei voi lyödä niin maila piiloitetaan.
        void cueSize()
        {
            if (CanHit == true)
            {
                cue.Size = new Vector(17, 328);
            }
            else
            {
                cue.Size = new Vector(1, 1);
            }
        }

    }

    /// <summary>
    /// Hiireen sidottu funktio, laskee Atan2:lla mailan ja pallon välisen kulman ja kääntää mailaa osoittamaan pallon suuntaan aina
    /// </summary>
    /// <param name="cue">Vastaanottaa beginnissä alustettu maila</param>
    /// <param name="whiteBall">Vastaanottaa beginissä alustettu valkoinen pallo</param>
    public void MoveCue(PhysicsObject cue, PhysicsObject whiteBall)
    {
        cue.Position = Mouse.PositionOnScreen;
        double posX = cue.Position.X - whiteBall.Position.X;
        double posY = cue.Position.Y - whiteBall.Position.Y;
        cue.Angle = Angle.FromRadians(Math.Atan2(posY, posX) + Math.PI / 2);
    }

    /// <summary>
    /// Hiireen sidottu funktio joka vastaa palloon lyömisestä
    /// </summary>
    /// <param name="whiteBall"></param>
    /// <param name="cue"></param>
    /// <param name="hitPower"></param>
    public void HitBall(PhysicsObject whiteBall, PhysicsObject cue, ref double hitPower)
    {
        //Luo uuden vektorin jonka arvoksi tulee valkoisen pallon sekä mailan sijaintien erotus karteesisella koordinaatistolla
        Vector suunta = new Vector(whiteBall.X - cue.X, whiteBall.Y - cue.Y);

        // Mikäli voidaan lyödä, normalisoidaan suunan voimavektori ja kerrotaan se hiirenpainalluksen kerryttämällä voimakertoimella
        if (CanHit == true)
        {
            whiteBall.Push(suunta.Normalize() * hitPower);
            pointCounter.Value -= 1;
        }
        
        // Jos ei voida lyödä, herjataan äänellä.
        else
        {
            Sfx.PlayError();
        }

    }

    /// <summary>
    /// Funktio joka suoritetaan mikäli kasipallo pussitetaan ennen muita palloja
    /// </summary>
    public void Fail()
    {
        MessageDisplay.Add("Hävisit pelin! Paina R-näppäintä aloittaaksesi alusta");
        Sfx.StopMusic();
        Sfx.PlayGameOver();
        Pause();
    }


    /// <summary>
    // Funktio, joka suoritetaan jos kasipallo pussitetaan viimeisenä
    /// </summary>
    public void Win()
    {
        Sfx.StopMusic();
        Sfx.PlayYouWin();
        MessageDisplay.Add("Voitto! Pisteesi ovat" + pointCounter.Value.ToString());
    }

    /// <summary>
    /// Alustaa pelin suorittamalla tarpeelliset funktiot resetin jälkeen.
    /// </summary>
    /// <param name="maila">Beginnissä alustettu maila</param>
    /// <param name="valkoinenPallo">Beginissä alustettu pallo</param>
    public void Init(PhysicsObject cue, PhysicsObject whiteBall)
    {
        CreateTable();
        CreateWhiteBall(whiteBall);
        CreateCue(cue);
        BindControls(cue, whiteBall);
        AddBalls(BallInitList());
        Collisions(whiteBall, BallsInGame);
        Updater(whiteBall);
        Sfx.PlayMusic();
        CreatePointCounter();
        BallsInGame.ForEach(ball => ball.Velocity = new Vector(0, 0));
    }

    /// <summary>
    /// Uudelleenkäynnistää pelin, pysäyttää musiikin ja suorittaa Init-funktion
    /// </summary>
    /// <param name="cue">Beginissä alustettu maila</param>
    /// <param name="whiteBall">Beginissä alustettu valkoinen pallo</param>
    public void Reset(PhysicsObject cue, PhysicsObject whiteBall)
    {
        // Asettaa jokaisen pallon velocityn nollaan, jotta resetin jälkeen CanHit päivittyy oikein.
        BallsInGame.ForEach(ball => ball.Velocity = new Vector(0, 0));
        ClearAll();
        RemoveCollisionHandlers();
        Sfx.StopMusic();
        // Suorittaa uusiksi aliohjelmat, jotka tuhottiin ClearAllilla.
        Init(cue, whiteBall);
    }
}
