import { useState, useEffect } from "react";
import { api } from "../api";
import { audio, useAudioState } from "../audio";

const SOUND_KEYS = [
  ["music", "Музыка / кассеты"],
  ["ambient", "Эмбиент в игре"],
  ["sfx", "Звуки интерфейса"],
];

export default function MainMenu({ playerName, canContinue, onNewGame, onContinue }) {
  const [modal, setModal] = useState(null); // "authors", "patchnotes", "load"
  const [soundOpen, setSoundOpen] = useState(false);
  const [saves, setSaves] = useState([]);
  const [loadingSaves, setLoadingSaves] = useState(false);
  const audioState = useAudioState();

  // Загружаем список сохранений, когда открываем модалку "load"
  useEffect(() => {
    if (modal === "load") {
      setLoadingSaves(true);
      api.getSaves()
        .then(setSaves)
        .catch(console.error)
        .finally(() => setLoadingSaves(false));
    }
  }, [modal]);

  const handleLoad = async (filename) => {
    try {
      await api.loadGame(filename);
      onContinue?.();
    } catch (e) {
      alert("Ошибка загрузки: " + e.message);
    }
  };

  const handleDelete = async (filename) => {
    if (!window.confirm(`Удалить сохранение ${filename}?`)) return;
    try {
      await api.deleteSave(filename);
      setSaves((list) => list.filter((s) => s.name !== filename));
    } catch (e) {
      alert("Ошибка удаления: " + e.message);
    }
  };

  const handleNewGame = () => {
    if (canContinue && !window.confirm("Начать новую игру? Текущий прогресс будет сброшен.")) return;
    onNewGame();
  };

  const saveLabel = (s) => {
    if (s.name === "autosave.json") return "🔄 Автосохранение";
    return s.name.replace(".json", "").replace("save_", "Слот ");
  };

  return (
    <div className="main-menu-bg">
      <div className="main-menu-overlay" />
      <div className="main-menu-container">
        <h1 className="game-title">РЕЙД</h1>
        <p className="game-subtitle">Выживи. Найди. Вернись.</p>

        <div className="menu-buttons">
          <button
            className="menu-btn"
            onClick={onContinue}
            disabled={!canContinue}
            title={canContinue ? `Продолжить за ${playerName || "выжившего"}` : "Нет сохранённого прогресса"}
          >
            Продолжить{canContinue && playerName ? ` (${playerName})` : ""}
          </button>

          <button className="menu-btn" onClick={handleNewGame}>
            Новая игра
          </button>

          <button className="menu-btn" onClick={() => setModal("load")}>
            Загрузить
          </button>

          <button
            className="menu-btn"
            onClick={() => {
              audio.setScene("menu");
              setSoundOpen(true);
            }}
          >
            🔊 Звук
          </button>

          <div className="menu-row">
            <button className="menu-btn small" onClick={() => setModal("authors")}>Авторы</button>
            <button className="menu-btn small" onClick={() => setModal("patchnotes")}>Патч-ноуты</button>
          </div>

          <a
            href="https://forms.gle/Lbki239TQbAnKyuR7"
            target="_blank"
            rel="noreferrer"
            className="menu-btn small danger"
            style={{ textDecoration: "none", textAlign: "center", display: "block" }}
          >
            🐞 Багрепорт
          </a>
        </div>

        <div className="version-tag">v1.0.6</div>
      </div>

      {/* Модалка Авторов */}
      {modal === "authors" && (
        <div className="modal-bg" onClick={() => setModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-head">
              <h3>Авторы</h3>
              <button className="close-btn" onClick={() => setModal(null)}>✖</button>
            </div>
            <div className="death-text">
              <b>Авторы:</b> Геолог, DeepSeek, Iceman<br /><br />
              <b>Помощники:</b> IvanBol, AngryPop, Мой отец<br /><br />
              <b>Специальная благодарность:</b> Моя сестра, Мой отец, преподаватель по ASP.NET и AJAX<br /><br />
              <b>Источники вдохновения и спрайтов:</b><br />
              S.T.A.L.K.E.R., Escape from Tarkov, DayZ, DayR, 7 Days To Die, Моды на S.T.A.L.K.E.R.
            </div>
          </div>
        </div>
      )}

      {/* Модалка Патч-ноутов */}
      {modal === "patchnotes" && (
        <div className="modal-bg" onClick={() => setModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-head">
              <h3>Патч-ноуты</h3>
              <button className="close-btn" onClick={() => setModal(null)}>✖</button>
            </div>
            <div className="death-text">
              <b>v1.0.6 (Текущая)</b>
              <ul>
                <li><b>Навыки/перки без дублей:</b> из перков убраны дублирующие <b>«Ловкость»</b> и <b>«Уклонение»</b> — они остались как <b>характеристики</b>. Эффекты объединены (ловкость даёт бонус к AP, уклонение — шанс увернуться); вложения в старые перки автоматически переносятся в характеристики при загрузке сохранения.</li>
                <li><b>Интерфейс:</b> характеристики перенесены из <b>«Статистики»</b> во вкладку <b>«Навыки»</b>, где разделены на «Характеристики» и «Перки». Очки характеристик/перков можно тратить, прокачка сохраняется.</li>
                <li><b>Исправления:</b> сохранение после улучшения перков, корректная загрузка очков/уровней характеристик из сохранений, правильный учёт ловкости в максимуме AP, раздельные подписи «Очки характеристик»/«Очки перков».</li>
              </ul>
              <hr />
              <b>v1.0.5</b>
              <ul>
                <li>Диалоги торговцев: интерактивная система в стиле S.T.A.L.K.E.R. — можно выбирать ответы, узнавать информацию и <b>брать задание прямо через диалог</b>.</li>
                <li>Торговцы/экономика: цены у Бармена (еда/напитки), Кузнеца (материалы), Скупщика (разное) и Медика (медицина) значительно повышены; продажа даёт <b>50%</b> от цены покупки у Бармена/Кузнеца/Медика и <b>25%</b> у Скупщика. Гриша переименован в <b>Кузнец</b>; вкладка Сидоровича — <b>Барахольщик</b> (имя персонажа сохранено).</li>
                <li>Бартер: обмен <b>только за ресурсы</b> (без доплаты деньгами).</li>
                <li>Доска объявлений: <b>1 задание на 1 торговца</b>; после выполнения/истечения срочного появляется новое случайное. У срочных больше не дублируется «СРОЧНО» и показывается срок на выполнение (игровое время).</li>
                <li>Гранаты: РГД-5/Ф-1/дымовая доступны к покупке и бартеру у Прапора. <b>Дымовая граната</b> в бою накрывает дымом — противники в радиусе пропускают 2 хода.</li>
                <li>Квестовые предметы: задания (например, записка/«Записи выжившего») теперь можно найти в рейде — предмет появляется в луте, пока задание активно и предмет не получен; при смерти квестовые/сюжетные предметы не теряются.</li>
                <li><b>Завод</b> открывается, когда задание «побывать на Заводе» <b>уже есть</b> (цепочка Бармена / сюжет) — локация доступна сразу, а для выполнения задания нужно сходить на Завод и выполнить его условие (побывать/выжить). Открывается не после цепочки Прапора.</li>
                <li>Исправления: принятие цепочек заданий торговцев в диалоге, минеральная вода у Бармена, аватарка «Усатый» (<code>usi</code>), патроны не помечаются как «из рейда» и не начисляются автоматически за убийство врага, квестовые предметы с боссов корректно засчитываются.</li>
              </ul>
              <hr />
              <b>v1.0.0</b>
              <ul>
                <li>Торговцы: во вкладке «Торговцы» играет случайная музыка (папка <code>public/audio/Traders</code>), эмбиент во время торговли не накладывается.</li>
                <li>Бартер: расширены предложения у торговцев; предметы, полученные по бартеру, считаются найденными в рейде («из рейда»).</li>
                <li>Задания: сюжет и цепочки торговцев открываются последовательно — видно только текущее/первое доступное задание, следующее появляется после выполнения.</li>
                <li>Доска объявлений: задания только по доступным локациям и от разблокированных торговцев; отображается заказчик (репутация при сдаче).</li>
                <li>Дневник: мысли записывает ГГ (автоматически), добавлена отдельная вкладка «Заметки» игрока и вкладка «Выполненные».</li>
                <li>Исправления: наложение эмбиента/музыки торговцев, видимость будущих заданий в диалогах/сюжете, отображение аватарки «Усатый» (<code>usi</code>), обновлён README.</li>
              </ul>
              <hr />
              <b>v0.6.0</b>
              <ul>
                <li>Переименованы и обновлены предметы экипировки (шлемы, костюмы, бронежилеты/разгрузки, рюкзаки), граната FDE-6 → РГД-5; подключены спрайты головных уборов.</li>
                <li>Новые разгрузки/бронежилеты с бонусом к слотам: Wassermann (+10), Охотничий жилет (+5), Commando (+8), 6Б13, «Корунд-ВМ». Пересчитаны названия и параметры рюкзаков/бронежилетов.</li>
                <li>Стартовая экипировка: персонаж появляется в зелёном свитере.</li>
                <li>Добавлены новые предметы: перекись водорода (лечит), запечатанный шприц, набор медикаментов, материалы/инструменты (отвёртки, шило, порох, паракорд, ткани, электроника, инструменты и др.), ценности и прочее (ключи, ювелирные изделия, золотой слиток, документы, техника, жетоны военных и др.).</li>
                <li>На локациях появились автомобили (🚗) — их можно обыскать.</li>
                <li>Вход в здание открывает интерьер (сетка, как бой, но без врагов, если здание мирное): можно ходить, обыскивать мебель/контейнеры и находить тайник внутри. Враждебные здания — бой. Поддержка спрайтов мебели через поле <code>sprite</code>.</li>
                <li>Добавлены типы домов: жилой дом, обветшалый дом, деревянный дом (от 1 до 2×1 клетки).</li>
              </ul>
              <hr />
              <b>v0.5.9</b>
              <ul>
                <li>Исправлены аватарки торговцев; вкладки: Медик (Сергей), Старьёвщик (Сидорович), Скупщик.</li>
                <li>Медик продаёт только медицину; «Бальзам „Золотая звезда“», иконки медицины, лояльность по силе лечения.</li>
                <li>Обновлены еда/напитки, мёд +5 ХП, бутерброд, мятные конфеты, чай (Бармен). Выход из рейда по клетке выхода, трассеры, доработки навыков.</li>
                <li>Мутанты — только на открытой местности, видят на 3 клетки, обзор перекрывают здания/деревья.</li>
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Модалка Загрузки */}
      {modal === "load" && (
        <div className="modal-bg" onClick={() => setModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-head">
              <h3>Загрузить игру</h3>
              <button className="close-btn" onClick={() => setModal(null)}>✖</button>
            </div>
            {loadingSaves ? (
              <p className="desc">Загрузка списка...</p>
            ) : saves.length === 0 ? (
              <p className="desc">Нет доступных сохранений.</p>
            ) : (
              <div className="save-list">
                {saves.map((s) => (
                  <div key={s.name} className="save-item">
                    <div className="save-info">
                      <b>{saveLabel(s)}</b>
                      <span>
                        {s.playerName ? `${s.playerName} · ур. ${s.level} · ` : ""}
                        {s.storyTotal > 0 ? `сюжет ${s.storyDone}/${s.storyTotal} · ` : ""}
                        {s.time}
                      </span>
                    </div>
                    <div className="save-actions">
                      <button className="btn-ok" onClick={() => handleLoad(s.name)}>
                        Загрузить
                      </button>
                      <button className="btn-danger" onClick={() => handleDelete(s.name)}>
                        🗑
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Модалка звука (доступна из главного меню, без входа в игру) */}
      {soundOpen && (
        <div className="modal-bg" onClick={() => setSoundOpen(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-head">
              <h3>🔊 Звук</h3>
              <button className="close-btn" onClick={() => setSoundOpen(false)}>✖</button>
            </div>
            <p className="desc">Громкость музыки, эмбиента и звуков. Меняется сразу, без входа в игру.</p>
            {SOUND_KEYS.map(([key, label]) => (
              <div className="volume-row" key={key}>
                <span>{label}</span>
                <input
                  type="range"
                  min="0"
                  max="100"
                  step="1"
                  value={Math.round(audioState.settings[key] * 100)}
                  onChange={(e) => audio.setVolume(key, Number(e.target.value) / 100)}
                />
                <b>{Math.round(audioState.settings[key] * 100)}%</b>
              </div>
            ))}
            <div className="combat-buttons">
              <button className="btn-ok" onClick={() => setSoundOpen(false)}>Готово</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
