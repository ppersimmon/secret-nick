import { createPortal } from "react-dom";
import Button from "@components/common/button/Button";
import IconButton from "@components/common/icon-button/IconButton";
import { ICONS_PATH } from "@components/common/icon-button/utils";
import "./ConfirmDeleteModal.scss";
import "@assets/styles/common/modal-container.scss";

interface ConfirmDeleteModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirmDelete: () => void;
  userName: string;
}

const ConfirmDeleteModal = ({
  isOpen,
  onClose,
  onConfirmDelete,
  userName,
}: ConfirmDeleteModalProps) => {
  if (!isOpen) return null;

  const modalElement = (
    <div className="modal-container">
      <div className="confirm-delete-modal">
        <div className="modal__header">
          <svg className="modal__picture">
            <use href={`${ICONS_PATH}gift`} />
          </svg>

          <h3 className="modal__title">{`Delete ${userName}?`}</h3>
          <div className="modal__close-button">
            <IconButton iconName="cross" onClick={onClose} />
          </div>
        </div>

        <p className="modal__description">
          Are you sure you want to remove the participant from this room? They
          will permanently lose access and all their data will be deleted. This
          action cannot be undone.
        </p>

        <div className="modal__actions">
          <Button variant="secondary" size="medium" onClick={onClose}>
            Cancel
          </Button>
          <Button variant="primary" size="medium" onClick={onConfirmDelete}>
            Delete
          </Button>
        </div>
      </div>
    </div>
  );

  return createPortal(modalElement, document.body);
};

export default ConfirmDeleteModal;
