using Jypeli;

// TODO LISÄÄ LYÖNTIÄÄNI JA PALLOJEN TÖRMÄYS
public class SFX
{
    readonly SoundEffect error = Game.LoadSoundEffect("false");
    readonly SoundEffect seina = Game.LoadSoundEffect("wall");
    readonly SoundEffect intro = Game.LoadSoundEffect("intro");
    readonly SoundEffect bg = Game.LoadSoundEffect("bg");
    readonly SoundEffect ball = Game.LoadSoundEffect("ball");
    readonly SoundEffect fail = Game.LoadSoundEffect("fail");
    readonly SoundEffect win = Game.LoadSoundEffect("win");

    private double volumeLevel = 0.2;

    public double VolumeLevel
    {
        get { return volumeLevel; }
        set { volumeLevel = value; }
    }

    public void PlayWall()
    {
        seina.Play(volumeLevel, 0, 0);
    }
    public void PlayError()
    {
        error.Play(volumeLevel, 0, 0);
    }

    public void PlayMusic()
    {
        intro.Play(0.0, 0, 0);
        Timer.SingleShot(6.79, delegate { bg.Play(0.0, 0, 0); });
        Timer.CreateAndStart(143.8, delegate { bg.Play(0.0, 0, 0); });
    }

    public void PlayBall()
    {
        ball.Play(volumeLevel, 0, 0);
    }

    public void PlayFail()
    {
        fail.Play(volumeLevel, 0, 0);
    }

    public void PlayWin()
    {
        win.Play(volumeLevel, 0, 0);
    }

    public void StopMusic()
    {
        intro.Stop();
        bg.Stop();
    }

}