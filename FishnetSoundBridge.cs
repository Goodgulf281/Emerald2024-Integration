using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using EmeraldAI;
using FishNet.Object;
using UnityEngine;


namespace EmeraldAI
{

    /*
     * This class acts as a bridge between the server where the AI runs and teh client where the sound from Emerald's Sound Profile needs to be played.
     * Currently it is setup for a dedicated server, see the UNITY_SERVER defines. 
     *
     * If you are running a fishent peer to peer setup you may want to change the these defines into checks using IsServerInitialized from the NetworkObject
     * attached to this gameObject.
     */
    
    public class FishnetSoundBridge : NetworkBehaviour
    {

        public enum SoundProfileCategory
        {
            Idle,
            Footstep,
            Interact,
            EquipType1,
            EquipType2,
            UnequipType1,
            UnequipType2,
            Attack,
            Injured,
            Block,
            Death,
            Warning
        };

        private AudioSource _audioSource;
        private AudioSource _secondaryAudioSource;
        private AudioSource _eventAudioSource;
        private EmeraldSounds _emeraldSounds;
        private Utility.EmeraldSoundProfile _soundProfile;
        private bool _clientInitialized = false;
        private bool _serverInitialized = false; 
        
        private void Awake()
        {
            Debug.Log("FishnetSoundBridge.Awake(): Event Called");
        }

        public void InitializeSoundBridge(EmeraldSounds emeraldSounds)
        {
            _emeraldSounds = emeraldSounds;
            _soundProfile = emeraldSounds.SoundProfile;
            
            _serverInitialized = true;
            
            // Alternatively, check for IsServerInitialized instead of using defines: 
#if UNITY_SERVER
            Debug.Log("FishnetSoundBridge.InitializeSoundBridge(): called on Server");
#else
            Debug.Log("FishnetSoundBridge.InitializeSoundBridge(): initializing on Client");
            InitializeSoundBridgeClientSide();
#endif
        }

        
        public override void OnStartClient()
        {
            base.OnStartClient();
            if(!_clientInitialized)
                InitializeSoundBridgeClientSide();
        }

        public void InitializeSoundBridgeClientSide()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                Debug.LogError($"FishnetSoundBridge.InitializeSoundBridgeClientSide(): no audio source found on client object {gameObject.name}");
                return;
            }
            
            Debug.Log($"FishnetSoundBridge.InitializeSoundBridgeClientSide(): event called");
            
            _secondaryAudioSource = gameObject.AddComponent<AudioSource>();
            _secondaryAudioSource.priority = _audioSource.priority;
            _secondaryAudioSource.spatialBlend = _audioSource.spatialBlend;
            _secondaryAudioSource.minDistance = _audioSource.minDistance;
            _secondaryAudioSource.maxDistance = _audioSource.maxDistance;
            _secondaryAudioSource.rolloffMode = _audioSource.rolloffMode;
            _eventAudioSource = gameObject.AddComponent<AudioSource>();
            _eventAudioSource.priority = _audioSource.priority;
            _eventAudioSource.spatialBlend = _audioSource.spatialBlend;
            _eventAudioSource.minDistance = _audioSource.minDistance;
            _eventAudioSource.maxDistance = _audioSource.maxDistance;
            _eventAudioSource.rolloffMode = _audioSource.rolloffMode;
            
            _clientInitialized = true;
        }

        public void PlayOneShot(SoundProfileCategory category, int index, bool walking=true)
        {
#if UNITY_SERVER
            Debug.Log($"FishnetSoundBridge.PlayOneShot(): called on Server for Category {category} and {index}");
            if (!_serverInitialized)
            {
                Debug.LogError($"FishnetSoundBridge.PlayOneShot(): server not initialized");
                return;
            }
            PlayOneShotClient(category, index, walking);
#else
            Debug.Log($"FishnetSoundBridge.PlayOneShot(): called on Client for Category {category} and {index}");
            if(!_clientInitialized)
            {
                Debug.LogError($"FishnetSoundBridge.PlayOneShot(): client not initialized");
                return;
            }
            PlayOneShotClientInternal(category, index, walking);
#endif            
        }

        [ObserversRpc]
        private void PlayOneShotClient(SoundProfileCategory category, int index, bool walking)
        {
            PlayOneShotClientInternal(category, index, walking);
        }

        private void PlayOneShotClientInternal(SoundProfileCategory category, int index, bool walking)
        {
            if (!_clientInitialized)
            {
                Debug.LogError($"FishnetSoundBridge.PlayOneShotClientInternal(): client side not initialized");
                return;
            }
            
            Debug.Log($"FishnetSoundBridge.PlayOneShotClientInternal(): "+'\u266B'.ToString() +$" called on Client for Category {category} and {index}");
            float volume = 0.0f;
            AudioClip clip = null;
            
            if (category == SoundProfileCategory.Idle)
            {
                if (index < _soundProfile.IdleSounds.Count)
                {
                    volume = _soundProfile.IdleVolume;
                    clip = _soundProfile.IdleSounds[index];
                }
            }
            else if (category == SoundProfileCategory.Footstep)
            {
                if (index < _soundProfile.FootStepSounds.Count)
                {
                    if (walking)
                    {
                        volume = _soundProfile.WalkFootstepVolume;
                    }
                    else 
                    {
                        volume = _soundProfile.RunFootstepVolume;
                    }
                    clip = _soundProfile.WarningSounds[index];
                }
            }
            else if (category == SoundProfileCategory.Interact)
            {
                if (index < _soundProfile.InteractSounds.Count)
                {
                    volume = 1.0f;
                    clip = _soundProfile.InteractSounds[index].SoundEffectClip;
                }
            }
            else if (category == SoundProfileCategory.EquipType1)
            {
                volume = _soundProfile.EquipVolume;
                clip = _soundProfile.UnsheatheWeapon;
            }
            else if (category == SoundProfileCategory.EquipType2)
            {
                volume = _soundProfile.EquipVolume;
                clip = _soundProfile.RangedUnsheatheWeapon;
            }
            else if (category == SoundProfileCategory.UnequipType1)
            {
                volume = _soundProfile.EquipVolume;
                clip = _soundProfile.SheatheWeapon;
            }
            else if (category == SoundProfileCategory.UnequipType2)
            {
                volume = _soundProfile.EquipVolume;
                clip = _soundProfile.RangedSheatheWeapon;
            }
            else if (category == SoundProfileCategory.Attack)
            {
                if (index < _soundProfile.AttackSounds.Count)
                {
                    volume = _soundProfile.AttackVolume;
                    clip = _soundProfile.AttackSounds[index];
                    _audioSource.pitch = Mathf.Round(Random.Range(0.9f, 1.1f) * 10) / 10;
                }
            }
            else if (category == SoundProfileCategory.Injured)
            {
                if (index < _soundProfile.InteractSounds.Count)
                {
                    volume = _soundProfile.InjuredVolume;
                    clip = _soundProfile.InjuredSounds[index];
                    _audioSource.pitch = Mathf.Round(Random.Range(0.8f, 1.1f) * 10) / 10;
                }
            }
            else if (category == SoundProfileCategory.Block)
            {
                if (index < _soundProfile.BlockingSounds.Count)
                {
                    volume = _soundProfile.BlockVolume;
                    clip = _soundProfile.BlockingSounds[index];
                    _audioSource.pitch = Mathf.Round(Random.Range(0.7f, 1.1f) * 10) / 10;
                }
            }
            else if (category == SoundProfileCategory.Death)
            {
                if (index < _soundProfile.DeathSounds.Count)
                {
                    volume = _soundProfile.DeathVolume;
                    clip = _soundProfile.DeathSounds[index];
                }
            }
            else if (category == SoundProfileCategory.Warning)
            {
                if (index < _soundProfile.WarningSounds.Count)
                {
                    volume = _soundProfile.WarningVolume;
                    clip = _soundProfile.WarningSounds[index];
                }
            }
            else
            {
                Debug.LogError("FishnetSoundBridge.PlayOneShotClient(): category not supported");
                return;
            }

            if (!_audioSource.isPlaying && clip != null)
            {
                _audioSource.volume = volume;
                _audioSource.PlayOneShot(clip);
                _audioSource.pitch = 1;
            }
            else if (!_secondaryAudioSource.isPlaying && clip != null)
            {
                _secondaryAudioSource.volume = volume;
                _secondaryAudioSource.PlayOneShot(clip);
            }
            else if(clip)
            {
                _eventAudioSource.volume = volume;
                _eventAudioSource.PlayOneShot(clip);
            }
            else Debug.LogError("FishnetSoundBridge.PlayOneShotClient(): clip is null");
        }

    }


}
