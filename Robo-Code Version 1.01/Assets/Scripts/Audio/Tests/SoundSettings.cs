public class SoundSettings
{
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    public float MusicVolume
    {
        get { return musicVolume; }
    }

    public float SFXVolume
    {
        get { return sfxVolume; }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Clamp(volume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Clamp(volume);
    }

    private float Clamp(float volume)
    {
        if (volume < 0f)
        {
            return 0f;
        }

        if (volume > 1f)
        {
            return 1f;
        }

        return volume;
    }
}
