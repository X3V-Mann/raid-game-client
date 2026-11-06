import { useEffect, useState, useCallback, useRef } from "react";
import { api } from "./api";
import PlayerPanel from "./components/PlayerPanel";
import Inventory from "./components/Inventory";
import Traders from "./components/Traders";
import Shelter from "./components/Shelter";
import Quests from "./components/Quests";
import MapTab from "./components/MapTab";
import Settings from "./components/Settings";
import AvatarPicker from "./components/AvatarPicker";
import Onboarding from "./components/Onboarding";
import TopBar from "./components/TopBar";
import Raid from "./components/raid/Raid";
import Saves from "./components/Saves";
import MainMenu from "./components/MainMenu";
import StatsTab from "./components/StatsTab";
import Perks from "./components/Perks";
import { applyTheme, loadTheme } from "./components/Settings";
import { audio } from "./audio";

export default function App() {
  const [inMenu, setInMenu] = useState(true);
  const [menuPlayer, setMenuPlayer] = useState(null);
  const [player, setPlayer] = useState(null);
  const [tab, setTab] = useState("map");
  const [invOpen, setInvOpen] = useState(false);
  const [avatarOpen, setAvatarOpen] = useState(false);
  const [inRaid, setInRaid] = useState(false);
  const [location, setLocation] = useState(null);
  const [toast, setToast] = useState("");
  const [endingSeen, setEndingSeen] = useState(() => {
    try {
      return localStorage.getItem("raid.endingSeen") === "1";
    } catch {
      return false;
    }
  });
  const prevLevel = useRef(null);

  useEffect(() => {
    applyTheme(loadTheme());
  }, []);

  useEffect(() => {
    // Музыка меню — только в меню, эмбиент — только в игре.
    audio.setScene(inMenu ? "menu" : "game");
  }, [inMenu]);

  const showToast = (msg) => {
    setToast(msg);
    setTimeout(() => setToast(""), 2500);
  };

  // Узнаём, есть ли сохранённый прогресс — для кнопки «Продолжить».
  useEffect(() => {
    api
      .getPlayer()
      .then(setMenuPlayer)
      .catch(() => {});
  }, []);

  const refresh = useCallback(async () => {
    const p = await api.getPlayer();
    setPlayer(p);
    setMenuPlayer(p);

    // Повышение уровня может произойти на сервере после синхронизации опыта.
    if (prevLevel.current != null && p.level > prevLevel.current) {
      setToast(`⬆ Новый уровень: ${p.level}`);
      setTimeout(() => setToast(""), 2500);
    }
    prevLevel.current = p.level;

    return p;
  }, []);

  const act = async (fn) => {
    try {
      await fn();
      await refresh();
    } catch (e) {
      showToast(e.message || "Ошибка");
    }
  };

  useEffect(() => {
    if (inMenu) return;

    if (!inRaid) refresh().catch(() => showToast("Сервер недоступен"));

    // Пока идёт рейд, состояние игрока живёт на клиенте (шаги, лут, бой),
    // поэтому опрос сервера и автосейв приостановлены — иначе они затрут прогресс рейда.
    const autoSave = setInterval(() => {
      if (!inRaid) api.autoSave?.().catch(() => {});
    }, 30_000);

    const tick = setInterval(() => {
      if (!inRaid) refresh().catch(() => {});
    }, 3000);

    return () => {
      clearInterval(autoSave);
      clearInterval(tick);
    };
  }, [refresh, inMenu, inRaid]);

  // Запуск игры (используется и для новой, и для загрузки, и для продолжения)
  const startGame = async () => {
    try {
      const p = await api.getPlayer();
      setPlayer(p);
      setMenuPlayer(p);
      prevLevel.current = p.level;
      setInMenu(false);
      setTab("map");
    } catch (e) {
      showToast(e.message || "Сервер недоступен");
    }
  };

  const newGame = async () => {
    try {
      try {
        localStorage.removeItem("raid.endingSeen");
      } catch {
        // ignore
      }
      setEndingSeen(false);
      await api.resetGame();
      await startGame();
    } catch (e) {
      showToast(e.message || "Не удалось начать новую игру");
    }
  };

  if (inMenu) {
    return (
      <MainMenu
        playerName={menuPlayer?.name}
        canContinue={!!menuPlayer?.onboarded}
        onNewGame={newGame}
        onContinue={startGame}
      />
    );
  }

  if (!player) return <div className="loading">Загрузка...</div>;

  if (!player.onboarded) {
    return (
      <Onboarding
        onDone={(updated) => {
          setPlayer(updated);
          setMenuPlayer(updated);
          showToast(`Добро пожаловать, ${updated.name}`);
        }}
      />
    );
  }

  const tabs = [
    ["map", "Карта"],
    ["traders", "Торговцы"],
    ["shelter", "Убежище"],
    ["story", "Сюжет"],
    ["perks", player.perkPoints > 0 ? `🧬 Навыки (${player.perkPoints})` : "🧬 Навыки"],
    ["stats", "📊 Статистика"],
    ["saves", "💾 Сохранения"],
    ["settings", "⚙ Настройки"],
  ];

  return (
    <div className="app">
      <div className="sidebar">
        <TopBar player={player} />
        <PlayerPanel
          player={player}
          onAvatarClick={() => setInvOpen(true)}
          onAvatarChange={() => setAvatarOpen(true)}
        />
        <nav>
          {tabs.map(([key, label]) => (
            <button
              key={key}
              className={tab === key ? "active" : ""}
              disabled={inRaid}
              onClick={() => setTab(key)}
            >
              {label}
            </button>
          ))}
        </nav>
        <div style={{ marginTop: "auto", padding: "10px" }}>
          <button className="menu-btn small" onClick={() => setInMenu(true)}>
            🚪 Выйти в меню
          </button>
        </div>
      </div>

      <main>
        {inRaid ? (
          <Raid
            player={player}
            location={location}
            onExit={async (updated) => {
              try {
                if (updated?.died) {
                  // Смерть уже обработана сервером в /player/die:
                  // не засчитываем выживание и не перезаписываем состояние клиентом.
                  // Но автосейв обновляем, иначе после перезапуска вернётся старая жизнь.
                  await api.autoSave();
                } else {
                  await api.syncPlayer(updated);
                  const alive = (updated?.hp ?? 0) > 0;
                  const cells = updated?.raidCells || 0;
                  // Клетки прошедшего рейда идут в статистику даже после провала —
                  // выживание при этом учитывается только по флагу «выжил/погиб».
                  // Рейд, который не начался (cells = 0), не идёт в статистику.
                  if (cells > 0) {
                    if (alive) await api.questProgress("survive", 1);
                    await api.syncStats(cells, alive);
                  }
                  await api.autoSave();
                }
              } catch (e) {
                console.error(e);
                showToast(e.message || "Не удалось сохранить итоги рейда");
              }

              setInRaid(false);
              setLocation(null);
              await refresh().catch(() => {});
            }}
            onToast={showToast}
            onPlayerChange={setPlayer}
          />
        ) : (
          <>
            {tab === "map" && (
              <MapTab
                onStartRaid={(loc) => {
                  setLocation(loc);
                  setInRaid(true);
                }}
              />
            )}
            {tab === "traders" && <Traders player={player} act={act} />}
            {tab === "shelter" && <Shelter player={player} act={act} />}
            {tab === "story" && <Quests story act={act} />}
            {tab === "perks" && <Perks act={act} onToast={showToast} />}
            {tab === "stats" && <StatsTab player={player} />}
            {tab === "saves" && <Saves onToast={showToast} refreshPlayer={refresh} />}
            {tab === "settings" && <Settings />}
          </>
        )}
      </main>

      {invOpen && (
        <Inventory
          player={player}
          act={act}
          onClose={() => setInvOpen(false)}
        />
      )}

      {avatarOpen && (
        <AvatarPicker
          current={player.avatar}
          onClose={() => setAvatarOpen(false)}
          onSelect={async (id) => {
            try {
              await api.setAvatar(id);
              await refresh();
              setAvatarOpen(false);
              showToast("Аватар обновлён");
            } catch (e) {
              showToast(e.message || "Ошибка");
            }
          }}
        />
      )}

      {toast && <div className="toast">{toast}</div>}

      {player.gameCompleted && !endingSeen && (
        <div className="ending-screen">
          <div className="ending-card">
            <h2>☢ КОНЕЦ</h2>
            <p>
              Ты нажал кнопку. Градирни исчезли в белой вспышке — вместе с ложью,
              ставками и теми, кто решил поиграть в войну.
            </p>
            <p>
              Дальше была только тишина. И она была честнее всего, что ты видел
              в этом регионе.
            </p>
            <div className="end-actions">
              <button
                className="menu-btn small"
                onClick={() => {
                  try {
                    localStorage.setItem("raid.endingSeen", "1");
                  } catch {
                    // ignore
                  }
                  setEndingSeen(true);
                }}
              >
                🔁 Свободная игра (Freeplay)
              </button>
              <button className="menu-btn small" onClick={() => setInMenu(true)}>
                В меню
              </button>
              <button className="menu-btn small danger" onClick={newGame}>
                Новая игра
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
